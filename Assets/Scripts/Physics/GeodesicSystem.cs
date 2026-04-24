using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace Vortex.Physics
{
    public sealed class GeodesicSystem : MonoBehaviour
    {
        [SerializeField] private bool autoCollectBodies = true;
        [SerializeField] private List<RelativisticBody> bodies = new List<RelativisticBody>();
        [SerializeField] private bool pauseSimulationWhenUnfocused = true;
        [SerializeField] private bool pauseSimulationWhenUnfocusedInEditor = false;
        [SerializeField, Min(0)] private int settleFixedStepsAfterFocusReturn = 2;

        private bool hasFocus = true;
        private int focusSettleStepsRemaining;

        private void Start()
        {
            if (autoCollectBodies)
            {
                RefreshBodies();
            }
        }

        private void FixedUpdate()
        {
            if (ShouldPauseForFocusLoss())
            {
                return;
            }

            if (focusSettleStepsRemaining > 0)
            {
                focusSettleStepsRemaining--;
                return;
            }

            if (autoCollectBodies)
            {
                RemoveNullBodies();
            }

            IReadOnlyList<GravityWell> wells = GravityWellRegistry.GetAll();
            float dt = Time.fixedUnscaledDeltaTime;

            int bodyCount = bodies.Count;
            if (bodyCount == 0)
            {
                return;
            }

            using NativeArray<GeodesicBodyStateData> currentStates = new NativeArray<GeodesicBodyStateData>(bodyCount, Allocator.TempJob);
            using NativeArray<GeodesicBodyStateData> nextStates = new NativeArray<GeodesicBodyStateData>(bodyCount, Allocator.TempJob);
            using NativeArray<float> localDeltaTimes = new NativeArray<float>(bodyCount, Allocator.TempJob);
            TransformAccessArray transforms = new TransformAccessArray(bodyCount);

            for (int i = 0; i < bodyCount; i++)
            {
                RelativisticBody body = bodies[i];
                if (body == null)
                {
                    continue;
                }

                Vector4 fourVelocity = body.FourVelocity;
                currentStates[i] = new GeodesicBodyStateData
                {
                    position = body.PhysicsPosition,
                    rotation = body.PhysicsRotation,
                    velocity = new Unity.Mathematics.float3(fourVelocity.x, fourVelocity.y, fourVelocity.z),
                    angularVelocityDegPerSec = body.AngularVelocityDegPerSec,
                    properTime = body.ProperTime,
                    inertialMass = body.InertialMass,
                    gravitationalMass = body.GravitationalMass,
                    contributesToGravity = (byte)(body.ContributesToGravity ? 1 : 0)
                };

                transforms.Add(body.transform);
            }

            using NativeArray<GravityWellData> wellData = new NativeArray<GravityWellData>(wells.Count, Allocator.TempJob);
            for (int i = 0; i < wells.Count; i++)
            {
                wellData[i] = wells[i] != null ? wells[i].ToData() : default;
            }

            GeodesicIntegrator integrateJob = new GeodesicIntegrator
            {
                currentStates = currentStates,
                nextStates = nextStates,
                localDeltaTimes = localDeltaTimes,
                wells = wellData,
                globalDeltaTime = dt,
                fixedDeltaTime = Time.fixedDeltaTime
            };

            JobHandle integrateHandle = integrateJob.Schedule(bodyCount, 32);

            GeodesicTransformApplyJob applyJob = new GeodesicTransformApplyJob
            {
                states = nextStates
            };

            JobHandle applyHandle = applyJob.Schedule(transforms, integrateHandle);
            applyHandle.Complete();

            for (int i = 0; i < bodyCount; i++)
            {
                RelativisticBody body = bodies[i];
                if (body == null)
                {
                    continue;
                }

                GeodesicBodyStateData state = nextStates[i];
                body.SetLocalDeltaTime(localDeltaTimes[i]);
                body.SetAngularVelocity(state.angularVelocityDegPerSec);

                Vector4 nextFourVelocity = body.FourVelocity;
                nextFourVelocity.x = state.velocity.x;
                nextFourVelocity.y = state.velocity.y;
                nextFourVelocity.z = state.velocity.z;
                body.SetPhysicsState(state.position, state.rotation, nextFourVelocity);
            }

            transforms.Dispose();
        }

        private void OnApplicationFocus(bool focus)
        {
            hasFocus = focus;
            if (focus)
            {
                focusSettleStepsRemaining = Mathf.Max(0, settleFixedStepsAfterFocusReturn);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            hasFocus = !paused;
            if (!paused)
            {
                focusSettleStepsRemaining = Mathf.Max(0, settleFixedStepsAfterFocusReturn);
            }
        }

        private bool ShouldPauseForFocusLoss()
        {
            if (!pauseSimulationWhenUnfocused || hasFocus)
            {
                return false;
            }

            if (Application.isEditor && !pauseSimulationWhenUnfocusedInEditor)
            {
                return false;
            }

            return true;
        }

        public void RefreshBodies()
        {
            bodies.Clear();
            RelativisticBody[] foundBodies = FindObjectsByType<RelativisticBody>(FindObjectsSortMode.None);
            for (int i = 0; i < foundBodies.Length; i++)
            {
                bodies.Add(foundBodies[i]);
            }
        }

        public void RegisterBody(RelativisticBody body)
        {
            if (body == null || bodies.Contains(body))
            {
                return;
            }

            bodies.Add(body);
        }

        public void UnregisterBody(RelativisticBody body)
        {
            if (body == null)
            {
                return;
            }

            bodies.Remove(body);
        }

        private void RemoveNullBodies()
        {
            for (int i = bodies.Count - 1; i >= 0; i--)
            {
                if (bodies[i] == null)
                {
                    bodies.RemoveAt(i);
                }
            }
        }
    }
}
