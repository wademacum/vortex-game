using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

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

            NativeArray<GeodesicBodyStateData> currentStates = new NativeArray<GeodesicBodyStateData>(bodyCount, Allocator.TempJob);
            NativeArray<GeodesicBodyStateData> nextStates = new NativeArray<GeodesicBodyStateData>(bodyCount, Allocator.TempJob);
            NativeArray<float> localDeltaTimes = new NativeArray<float>(bodyCount, Allocator.TempJob);

            try
            {

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
            }

            NativeArray<GravityWellData> wellData = new NativeArray<GravityWellData>(wells.Count, Allocator.TempJob);
            try
            {
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
            integrateHandle.Complete();

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

                Vector3 resolvedPosition = state.position;
                Vector3 velocity3 = new Vector3(state.velocity.x, state.velocity.y, state.velocity.z);
                Quaternion rotation = state.rotation;
                StructuralResponseBody structural = body.StructuralResponse;
                MeshNodeDeformer deformer = body.MeshDeformer;
                float dtSafe = Mathf.Max(PhysicsConstants.IntegrationEpsilon, dt);
                float bodyRadius = ResolveBodyRadius(body);
                float strongestTidal = 0f;
                Vector3 strongestAxis = Vector3.up;

                for (int w = 0; w < wells.Count; w++)
                {
                    GravityWell well = wells[w];
                    if (well == null)
                    {
                        continue;
                    }

                    if (IsSelfWell(body, well))
                    {
                        continue;
                    }

                    bool contacted;
                    Vector3 contactPos;
                    Vector3 contactNormal;
                    if (body.BodyCollider != null)
                    {
                        contacted = well.TryResolveSurfaceContact(body.BodyCollider, rotation, resolvedPosition, out contactPos, out contactNormal);
                    }
                    else
                    {
                        contacted = well.TryResolveSurfaceContact(resolvedPosition, out contactPos, out contactNormal);
                    }

                    if (contacted)
                    {
                        float penetrationDepth = (contactPos - resolvedPosition).magnitude;
                        float closingSpeed = Mathf.Max(0f, -Vector3.Dot(velocity3, contactNormal));

                        if (structural != null)
                        {
                            structural.ApplyContactStress(penetrationDepth, closingSpeed, dtSafe);
                        }

                        resolvedPosition = contactPos;
                        float radialVel = Vector3.Dot(velocity3, contactNormal);
                        if (radialVel < 0f)
                        {
                            velocity3 -= contactNormal * radialVel;
                        }
                    }

                    if (structural != null)
                    {
                        float tidal = ComputeTidalAcceleration(well, resolvedPosition, bodyRadius);
                        structural.ApplyTidalStress(tidal, dtSafe);

                        if (tidal > strongestTidal)
                        {
                            strongestTidal = tidal;
                            strongestAxis = (well.transform.position - resolvedPosition).normalized;
                        }
                    }
                    else if (deformer != null)
                    {
                        float tidal = ComputeTidalAcceleration(well, resolvedPosition, bodyRadius);
                        if (tidal > strongestTidal)
                        {
                            strongestTidal = tidal;
                            strongestAxis = (well.transform.position - resolvedPosition).normalized;
                        }
                    }
                }

                if (structural != null)
                {
                    structural.StepSimulation(dtSafe);
                }

                if (deformer != null)
                {
                    if (strongestAxis.sqrMagnitude <= PhysicsConstants.IntegrationEpsilon)
                    {
                        strongestAxis = Vector3.up;
                    }

                    deformer.ApplyTidalField(strongestAxis, strongestTidal, dtSafe);
                }

                Vector4 nextFourVelocity = body.FourVelocity;
                nextFourVelocity.x = velocity3.x;
                nextFourVelocity.y = velocity3.y;
                nextFourVelocity.z = velocity3.z;
                body.SetPhysicsState(resolvedPosition, rotation, nextFourVelocity);
            }
            }
            finally
            {
                wellData.Dispose();
            }
            }
            finally
            {
                localDeltaTimes.Dispose();
                nextStates.Dispose();
                currentStates.Dispose();
            }
        }

        private static bool IsSelfWell(RelativisticBody body, GravityWell well)
        {
            if (body == null || well == null)
            {
                return false;
            }

            if (body.transform == well.transform)
            {
                return true;
            }

            Collider bodyCollider = body.BodyCollider;
            Collider wellCollider = well.SurfaceCollider;
            if (bodyCollider != null && wellCollider != null)
            {
                if (bodyCollider == wellCollider)
                {
                    return true;
                }

                if (bodyCollider.transform.IsChildOf(well.transform) || wellCollider.transform.IsChildOf(body.transform))
                {
                    return true;
                }
            }

            return false;
        }

        private static float ResolveBodyRadius(RelativisticBody body)
        {
            if (body == null)
            {
                return 0.5f;
            }

            Collider col = body.BodyCollider;
            if (col != null)
            {
                Vector3 ext = col.bounds.extents;
                float r = Mathf.Max(ext.x, Mathf.Max(ext.y, ext.z));
                return Mathf.Max(PhysicsConstants.IntegrationEpsilon, r);
            }

            Vector3 scale = body.transform.lossyScale;
            float approx = 0.5f * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
            return Mathf.Max(PhysicsConstants.IntegrationEpsilon, approx);
        }

        private static float ComputeTidalAcceleration(GravityWell well, Vector3 position, float bodyRadius)
        {
            Vector3 delta = position - well.transform.position;
            float r = Mathf.Max(delta.magnitude, Mathf.Max(well.PhysicalRadius, PhysicsConstants.IntegrationEpsilon));
            float r3 = r * r * r;
            if (r3 <= PhysicsConstants.IntegrationEpsilon)
            {
                return 0f;
            }

            return 2f * PhysicsConstants.GravitationalConstant * well.Mass * bodyRadius / r3;
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
