using UnityEngine;

namespace Vortex.Physics
{
    public sealed class RelativisticBody : MonoBehaviour
    {
        [Header("Time State")]
        [SerializeField, Range(0f, 1f)] private float properTime = 1f;
        [SerializeField] private float localDeltaTime;
        [SerializeField] private float coordinateTime;

        [Header("Kinematics")]
        [SerializeField] private Vector3 sphericalPosition;
        [SerializeField] private Vector4 fourVelocity;

        private float cachedProperTime = 1f;

        public float ProperTime => properTime;
        public float LocalDeltaTime => localDeltaTime;
        public float CoordinateTime => coordinateTime;
        public Vector3 SphericalPosition
        {
            get => sphericalPosition;
            set => sphericalPosition = value;
        }

        public Vector4 FourVelocity
        {
            get => fourVelocity;
            set => fourVelocity = value;
        }

        public bool IsTimeFrozen => properTime <= 0f;

        private void Awake()
        {
            EnsureNoRigidbody();
            properTime = Mathf.Clamp01(properTime);
            cachedProperTime = properTime > 0f ? properTime : 1f;
        }

        private void OnValidate()
        {
            properTime = Mathf.Clamp01(properTime);
            if (properTime > 0f)
            {
                cachedProperTime = properTime;
            }

            #if UNITY_EDITOR
            // Notify trajectory preview about changes
            var preview = GetComponent<Vortex.Debugging.TrajectoryPreview>();
            if (preview != null)
            {
                preview.OnValidateFromBody();
            }
            #endif
        }

        private void Update()
        {
            coordinateTime += Time.deltaTime;
            localDeltaTime = Time.deltaTime * properTime;
        }

        public void FreezeProperTime()
        {
            if (properTime > 0f)
            {
                cachedProperTime = properTime;
            }

            properTime = 0f;
            localDeltaTime = 0f;
        }

        public void RestoreProperTime()
        {
            properTime = Mathf.Clamp01(cachedProperTime > 0f ? cachedProperTime : 1f);
        }

        [ContextMenu("Debug/Freeze Proper Time")]
        private void DebugFreezeProperTime()
        {
            FreezeProperTime();
        }

        [ContextMenu("Debug/Restore Proper Time")]
        private void DebugRestoreProperTime()
        {
            RestoreProperTime();
        }

        public void SetProperTimeScale(float value)
        {
            properTime = Mathf.Clamp01(value);
            if (properTime > 0f)
            {
                cachedProperTime = properTime;
            }
        }

        public void SetLocalDeltaTime(float value)
        {
            localDeltaTime = Mathf.Max(0f, value);
        }

        private void EnsureNoRigidbody()
        {
            Rigidbody rb3D = GetComponent<Rigidbody>();
            if (rb3D != null)
            {
                rb3D.isKinematic = true;
                rb3D.detectCollisions = false;
                rb3D.useGravity = false;
            }

            Rigidbody2D rb2D = GetComponent<Rigidbody2D>();
            if (rb2D != null)
            {
                rb2D.simulated = false;
            }
        }
    }
}
