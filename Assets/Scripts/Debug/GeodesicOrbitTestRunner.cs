using System.Collections;
using UnityEngine;
using Vortex.Physics;

namespace Vortex.Debugging
{
    [DisallowMultipleComponent]
    public sealed class GeodesicOrbitTestRunner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RelativisticBody targetBody;
        [SerializeField] private GravityWell primaryWell;

        [Header("Scenario Velocities")]
        [SerializeField] private Vector3 lowSpeedVelocity = new Vector3(0f, 0f, 8f);
        [SerializeField] private Vector3 highSpeedVelocity = new Vector3(0f, 0f, 38f);

        [Header("Freeze Test")]
        [SerializeField, Min(0.05f)] private float freezeDurationSeconds = 2f;
        [SerializeField, Min(0f)] private float freezePositionTolerance = 0.05f;

        [Header("NaN/Infinity Guard")]
        [SerializeField] private bool logNaNAndInfinity = true;

        private bool hasLoggedNumericFailure;

        private void Awake()
        {
            TryAutoAssign();
        }

        private void Update()
        {
            if (!logNaNAndInfinity || targetBody == null || hasLoggedNumericFailure)
            {
                return;
            }

            Vector3 p = targetBody.PhysicsPosition;
            Vector4 v = targetBody.FourVelocity;
            if (HasInvalid(p.x) || HasInvalid(p.y) || HasInvalid(p.z) ||
                HasInvalid(v.x) || HasInvalid(v.y) || HasInvalid(v.z) || HasInvalid(v.w))
            {
                hasLoggedNumericFailure = true;
                Debug.LogError("[GeodesicOrbitTestRunner] NaN/Infinity detected in body state.", this);
            }
        }

        [ContextMenu("Test/Apply Low Speed Scenario")]
        public void ApplyLowSpeedScenario()
        {
            TryAutoAssign();
            ApplyVelocity(lowSpeedVelocity);
            Debug.Log("[GeodesicOrbitTestRunner] Low-speed scenario applied.", this);
        }

        [ContextMenu("Test/Apply High Speed Scenario")]
        public void ApplyHighSpeedScenario()
        {
            TryAutoAssign();
            ApplyVelocity(highSpeedVelocity);
            Debug.Log("[GeodesicOrbitTestRunner] High-speed scenario applied.", this);
        }

        [ContextMenu("Test/Run Freeze ProperTime Check")]
        public void RunFreezeProperTimeCheck()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[GeodesicOrbitTestRunner] Freeze check can only run in Play mode.", this);
                return;
            }

            TryAutoAssign();
            if (targetBody == null)
            {
                Debug.LogWarning("[GeodesicOrbitTestRunner] Target body is missing.", this);
                return;
            }

            StopAllCoroutines();
            StartCoroutine(FreezeCheckRoutine());
        }

        private IEnumerator FreezeCheckRoutine()
        {
            Vector3 before = targetBody.PhysicsPosition;
            targetBody.FreezeProperTime();

            float elapsed = 0f;
            while (elapsed < freezeDurationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Vector3 after = targetBody.PhysicsPosition;
            float drift = Vector3.Distance(before, after);
            targetBody.RestoreProperTime();

            if (drift <= freezePositionTolerance)
            {
                Debug.Log($"[GeodesicOrbitTestRunner] Freeze PASS. Drift={drift:F5}", this);
            }
            else
            {
                Debug.LogError($"[GeodesicOrbitTestRunner] Freeze FAIL. Drift={drift:F5}", this);
            }
        }

        private void ApplyVelocity(Vector3 velocity)
        {
            if (targetBody == null)
            {
                return;
            }

            Vector4 fv = targetBody.FourVelocity;
            fv.x = velocity.x;
            fv.y = velocity.y;
            fv.z = velocity.z;
            targetBody.FourVelocity = fv;

            hasLoggedNumericFailure = false;
        }

        private void TryAutoAssign()
        {
            if (targetBody == null)
            {
                targetBody = GetComponent<RelativisticBody>();
            }

            if (primaryWell == null)
            {
                primaryWell = FindFirstObjectByType<GravityWell>();
            }
        }

        private static bool HasInvalid(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value);
        }
    }
}
