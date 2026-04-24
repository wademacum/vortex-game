using System.Collections.Generic;
using UnityEngine;
using Vortex.Physics;

namespace Vortex.Debugging
{
    [DisallowMultipleComponent]
    public sealed class GeodesicTestHarness : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GravityWell primaryWell;
        [SerializeField] private RelativisticBody referenceBody;

        [Header("Single Orbit Setup")]
        [SerializeField, Min(1f)] private float orbitRadius = 260f;
        [SerializeField, Min(0f)] private float tangentialVelocityScale = 1f;

        [Header("Stress Setup")]
        [SerializeField, Min(1)] private int stressBodyCount = 50;
        [SerializeField, Min(5f)] private float stressSpawnInnerRadius = 300f;
        [SerializeField, Min(10f)] private float stressSpawnOuterRadius = 540f;
        [SerializeField, Min(0f)] private float stressSpeedScale = 1f;

        private readonly List<GameObject> spawnedStressObjects = new List<GameObject>();

        [ContextMenu("Test/Setup Single Orbit Body")]
        public void SetupSingleOrbitBody()
        {
            if (!TryResolveReferences())
            {
                return;
            }

            Vector3 center = primaryWell.transform.position;
            Vector3 radialDir = Vector3.right;
            Vector3 tangentDir = Vector3.Cross(Vector3.up, radialDir).normalized;

            Vector3 position = center + radialDir * orbitRadius;
            referenceBody.transform.position = position;

            float circularSpeed = Mathf.Sqrt(
                Mathf.Max(PhysicsConstants.IntegrationEpsilon,
                    PhysicsConstants.GravitationalConstant * primaryWell.Mass / Mathf.Max(orbitRadius, PhysicsConstants.IntegrationEpsilon)));

            Vector3 velocity = tangentDir * circularSpeed * tangentialVelocityScale;
            Vector4 fv = referenceBody.FourVelocity;
            fv.x = velocity.x;
            fv.y = velocity.y;
            fv.z = velocity.z;
            referenceBody.FourVelocity = fv;

            Debug.Log($"[GeodesicTestHarness] Single orbit ready. Radius={orbitRadius:F1}, Speed={velocity.magnitude:F3}", this);
        }

        [ContextMenu("Test/Spawn Stress Bodies")]
        public void SpawnStressBodies()
        {
            if (!TryResolveReferences())
            {
                return;
            }

            ClearStressBodies();

            Vector3 center = primaryWell.transform.position;
            for (int i = 0; i < stressBodyCount; i++)
            {
                float t = (i + 0.5f) / stressBodyCount;
                float radius = Mathf.Lerp(stressSpawnInnerRadius, stressSpawnOuterRadius, t);
                float angle = (360f / stressBodyCount) * i;

                Vector3 radialDir = Quaternion.Euler(0f, angle, 0f) * Vector3.right;
                Vector3 tangentDir = Vector3.Cross(Vector3.up, radialDir).normalized;

                Vector3 spawnPos = center + radialDir * radius;
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"StressBody_{i:D2}";
                go.transform.position = spawnPos;
                go.transform.localScale = Vector3.one * 2f;

                RelativisticBody body = go.AddComponent<RelativisticBody>();
                body.SetProperTimeScale(1f);

                float circularSpeed = Mathf.Sqrt(
                    Mathf.Max(PhysicsConstants.IntegrationEpsilon,
                        PhysicsConstants.GravitationalConstant * primaryWell.Mass / Mathf.Max(radius, PhysicsConstants.IntegrationEpsilon)));

                Vector3 velocity = tangentDir * circularSpeed * stressSpeedScale;
                Vector4 fv = body.FourVelocity;
                fv.x = velocity.x;
                fv.y = velocity.y;
                fv.z = velocity.z;
                body.FourVelocity = fv;

                spawnedStressObjects.Add(go);
            }

            Debug.Log($"[GeodesicTestHarness] Spawned {spawnedStressObjects.Count} stress bodies.", this);
        }

        [ContextMenu("Test/Clear Stress Bodies")]
        public void ClearStressBodies()
        {
            for (int i = 0; i < spawnedStressObjects.Count; i++)
            {
                GameObject go = spawnedStressObjects[i];
                if (go == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(go);
                }
                else
                {
                    DestroyImmediate(go);
                }
            }

            spawnedStressObjects.Clear();
        }

        private bool TryResolveReferences()
        {
            if (primaryWell == null)
            {
                primaryWell = FindFirstObjectByType<GravityWell>();
            }

            if (referenceBody == null)
            {
                referenceBody = FindFirstObjectByType<RelativisticBody>();
            }

            if (primaryWell == null)
            {
                Debug.LogWarning("[GeodesicTestHarness] Primary GravityWell is missing.", this);
                return false;
            }

            if (referenceBody == null)
            {
                Debug.LogWarning("[GeodesicTestHarness] Reference RelativisticBody is missing.", this);
                return false;
            }

            return true;
        }
    }
}
