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
        [SerializeField] private Collider surfaceCollider;
        [SerializeField] private bool autoAssignSurfaceCollider = true;

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
        public Collider SurfaceCollider => surfaceCollider;

        private void Awake()
        {
            TryAutoAssignSurfaceCollider();
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
            TryAutoAssignSurfaceCollider();
            SyncRadiusFromVisualIfNeeded();
            Recalculate();
        }

        public void ApplyProceduralBody(float generatedMass, float generatedRadius)
        {
            mass = Mathf.Max(0f, generatedMass);
            physicalRadius = Mathf.Max(0f, generatedRadius);
            Recalculate();
        }

        public bool TryResolveSurfaceContact(Vector3 position, out Vector3 resolvedPosition, out Vector3 surfaceNormal)
        {
            resolvedPosition = position;
            surfaceNormal = Vector3.up;

            if (!enableSurfaceCollision)
            {
                return false;
            }

            if (surfaceCollider != null)
            {
                return TryResolveSurfaceContactWithCollider(position, out resolvedPosition, out surfaceNormal);
            }

            return TryResolveSurfaceContactWithRadius(position, out resolvedPosition, out surfaceNormal);
        }

        public bool TryResolveSurfaceContact(Collider bodyCollider, Quaternion bodyRotation, Vector3 bodyPosition, out Vector3 resolvedPosition, out Vector3 surfaceNormal)
        {
            resolvedPosition = bodyPosition;
            surfaceNormal = Vector3.up;

            if (!enableSurfaceCollision)
            {
                return false;
            }

            if (surfaceCollider != null && bodyCollider != null)
            {
                return TryResolveSurfaceContactWithCollider(bodyCollider, bodyRotation, bodyPosition, out resolvedPosition, out surfaceNormal);
            }

            return TryResolveSurfaceContact(bodyPosition, out resolvedPosition, out surfaceNormal);
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
                schwarzschildRadius = schwarzschildRadius,
                physicalRadius = physicalRadius
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

        private void TryAutoAssignSurfaceCollider()
        {
            if (!autoAssignSurfaceCollider || surfaceCollider != null)
            {
                return;
            }

            surfaceCollider = GetComponentInChildren<MeshCollider>();
            if (surfaceCollider == null)
            {
                surfaceCollider = GetComponentInChildren<Collider>();
            }
        }

        private bool TryResolveSurfaceContactWithCollider(Vector3 position, out Vector3 resolvedPosition, out Vector3 surfaceNormal)
        {
            resolvedPosition = position;
            surfaceNormal = Vector3.up;

            Vector3 center = transform.position;
            Vector3 radial = position - center;
            if (radial.sqrMagnitude <= PhysicsConstants.IntegrationEpsilon)
            {
                radial = Vector3.up;
            }

            Vector3 radialDirection = radial.normalized;
            float probeDistance = Mathf.Max(physicalRadius * 4f, 1f);
            Vector3 outsideProbe = center + radialDirection * probeDistance;
            Vector3 surfacePoint = surfaceCollider.ClosestPoint(outsideProbe);

            float pointRadial = Vector3.Dot(position - center, radialDirection);
            float surfaceRadial = Vector3.Dot(surfacePoint - center, radialDirection);
            if (pointRadial >= surfaceRadial)
            {
                return false;
            }

            Vector3 normal = outsideProbe - surfacePoint;
            if (normal.sqrMagnitude <= PhysicsConstants.IntegrationEpsilon)
            {
                normal = radialDirection;
            }
            else
            {
                normal.Normalize();
            }

            resolvedPosition = surfacePoint + normal * PhysicsConstants.SurfaceContactOffset;
            surfaceNormal = normal;
            return true;
        }

        private bool TryResolveSurfaceContactWithCollider(Collider bodyCollider, Quaternion bodyRotation, Vector3 bodyPosition, out Vector3 resolvedPosition, out Vector3 surfaceNormal)
        {
            resolvedPosition = bodyPosition;
            surfaceNormal = Vector3.up;

                if (!UnityEngine.Physics.ComputePenetration(
                    bodyCollider,
                    bodyPosition,
                    bodyRotation,
                    surfaceCollider,
                    surfaceCollider.transform.position,
                    surfaceCollider.transform.rotation,
                    out Vector3 direction,
                    out float distance))
            {
                return false;
            }

            if (distance <= PhysicsConstants.IntegrationEpsilon)
            {
                return false;
            }

            resolvedPosition = bodyPosition + direction * (distance + PhysicsConstants.SurfaceContactOffset);
            surfaceNormal = direction;
            return true;
        }

        private bool TryResolveSurfaceContactWithRadius(Vector3 position, out Vector3 resolvedPosition, out Vector3 surfaceNormal)
        {
            resolvedPosition = position;
            surfaceNormal = Vector3.up;

            float contactRadius = Mathf.Max(physicalRadius, schwarzschildRadius + PhysicsConstants.IntegrationEpsilon);
            if (contactRadius <= PhysicsConstants.IntegrationEpsilon)
            {
                return false;
            }

            Vector3 fromCenter = position - transform.position;
            float distance = fromCenter.magnitude;
            if (distance >= contactRadius)
            {
                return false;
            }

            Vector3 normal = distance > PhysicsConstants.IntegrationEpsilon
                ? fromCenter / distance
                : Vector3.up;

            resolvedPosition = transform.position + normal * (contactRadius + PhysicsConstants.SurfaceContactOffset);
            surfaceNormal = normal;
            return true;
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
