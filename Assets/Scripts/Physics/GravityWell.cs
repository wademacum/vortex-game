using UnityEngine;

namespace Vortex.Physics
{
    public enum RadiusSyncMode
    {
        Manual,
        RendererBounds,
        TransformScale
    }

    [DisallowMultipleComponent]
    public sealed class GravityWell : MonoBehaviour
    {
        [Header("Mass Settings")]
        [SerializeField, Min(0f)] private float mass = 88200f;
        [SerializeField, Min(0f)] private float physicalRadius = 200f;
        [SerializeField] private bool enableSurfaceCollision = false;

        [Header("Radius Sync")]
        [SerializeField] private RadiusSyncMode radiusSyncMode = RadiusSyncMode.RendererBounds;
        [SerializeField] private bool continuousRuntimeSync = false;
        [SerializeField, Min(0.01f)] private float transformScaleBaseDiameter = 1f;

        [Header("Computed")]
        [SerializeField, Min(0f)] private float schwarzschildRadius;

        public float Mass => mass;
        public float PhysicalRadius => physicalRadius;
        public float SchwarzschildRadius => schwarzschildRadius;
        public bool EnableSurfaceCollision => enableSurfaceCollision;

        private void Awake()
        {
            SyncRadiusFromVisualIfNeeded();
            Recalculate();
        }

        private void Update()
        {
            if (!continuousRuntimeSync || radiusSyncMode == RadiusSyncMode.Manual)
            {
                return;
            }

            float previous = physicalRadius;
            SyncRadiusFromVisualIfNeeded();
            if (!Mathf.Approximately(previous, physicalRadius))
            {
                Recalculate();
            }
        }

        private void OnEnable()
        {
            GravityWellRegistry.Register(this);
        }

        private void OnDisable()
        {
            GravityWellRegistry.Unregister(this);
        }

        private void OnValidate()
        {
            SyncRadiusFromVisualIfNeeded();
            Recalculate();
        }

        public void ApplyProceduralBody(float generatedMass, float generatedRadius)
        {
            mass = Mathf.Max(0f, generatedMass);
            physicalRadius = Mathf.Max(0f, generatedRadius);
            Recalculate();
        }

        public void RefreshRadiusFromVisual()
        {
            SyncRadiusFromVisualIfNeeded();
            Recalculate();
        }

        public GravityWellData ToData()
        {
            return new GravityWellData
            {
                position = transform.position,
                mass = mass,
                schwarzschildRadius = schwarzschildRadius
            };
        }

        private void Recalculate()
        {
            mass = Mathf.Max(0f, mass);
            physicalRadius = Mathf.Max(0f, physicalRadius);
            transformScaleBaseDiameter = Mathf.Max(0.01f, transformScaleBaseDiameter);

            float rs = 0f;
            if (mass > 0f)
            {
                float c2 = PhysicsConstants.SpeedOfLight * PhysicsConstants.SpeedOfLight;
                rs = 2f * PhysicsConstants.GravitationalConstant * mass / c2;
            }

            float maxAllowed = Mathf.Max(0f, physicalRadius - 10f);
            schwarzschildRadius = Mathf.Min(rs, maxAllowed);
        }

        private void SyncRadiusFromVisualIfNeeded()
        {
            if (radiusSyncMode == RadiusSyncMode.Manual)
            {
                return;
            }

            if (TryResolveVisualRadius(out float resolvedRadius))
            {
                physicalRadius = Mathf.Max(0f, resolvedRadius);
            }
        }

        private bool TryResolveVisualRadius(out float resolvedRadius)
        {
            resolvedRadius = physicalRadius;

            if (radiusSyncMode == RadiusSyncMode.RendererBounds)
            {
                Renderer targetRenderer = GetComponentInChildren<Renderer>();
                if (targetRenderer == null)
                {
                    return false;
                }

                Vector3 extents = targetRenderer.bounds.extents;
                resolvedRadius = Mathf.Max(extents.x, extents.y, extents.z);
                return true;
            }

            float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            resolvedRadius = 0.5f * transformScaleBaseDiameter * maxScale;
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (enableSurfaceCollision)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, physicalRadius);
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, schwarzschildRadius);
        }
    }
}
