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

        [Header("Intrinsic Spin")]
        [SerializeField] private Vector3 intrinsicSpinAxis = Vector3.up;
        [SerializeField, Min(0f)] private float intrinsicSpinDegPerSec;

        [Header("Mass")]
        [SerializeField, Min(0.001f)] private float inertialMass = 1f;
        [SerializeField, Min(0f)] private float gravitationalMass = 1f;
        [SerializeField] private bool contributesToGravity = false;

        [Header("Collision Response")]
        [SerializeField, Min(0f)] private float collisionSpinScale = 1f;
        [SerializeField, Min(0f)] private float collisionSpinImpulseScale = 90f;
        [SerializeField, Range(0f, 1f)] private float angularDampingPerFixedStep = 0.985f;

        [Header("Rendering")]
        [SerializeField] private bool interpolateRenderTransform = true;

        private float cachedProperTime = 1f;
        private Collider cachedCollider;
        private StructuralResponseBody cachedStructuralResponse;
        private MeshNodeDeformer cachedMeshDeformer;
        private Vector3 previousPhysicsPosition;
        private Vector3 currentPhysicsPosition;
        private Quaternion previousPhysicsRotation;
        private Quaternion currentPhysicsRotation;
        private Vector3 angularVelocityDegPerSec;

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
        public Collider BodyCollider => cachedCollider;
        public StructuralResponseBody StructuralResponse => cachedStructuralResponse;
        public MeshNodeDeformer MeshDeformer => cachedMeshDeformer;
        public float CollisionSpinScale => collisionSpinScale;
        public float CollisionSpinImpulseScale => collisionSpinImpulseScale;
        public float AngularDampingPerFixedStep => angularDampingPerFixedStep;
        public float InertialMass => Mathf.Max(0.001f, inertialMass);
        public float GravitationalMass => Mathf.Max(0f, gravitationalMass);
        public bool ContributesToGravity => contributesToGravity && GravitationalMass > 0f;
        public Vector3 PhysicsPosition => currentPhysicsPosition;
        public Quaternion PhysicsRotation => currentPhysicsRotation;
        public Vector3 AngularVelocityDegPerSec
        {
            get => angularVelocityDegPerSec;
            set => angularVelocityDegPerSec = value;
        }
        public Vector3 IntrinsicAngularVelocityDegPerSec => ResolveIntrinsicSpinAxis() * intrinsicSpinDegPerSec;

        private void Awake()
        {
            EnsureNoRigidbody();
            CacheCollider();
            CacheStructuralResponse();
            CacheMeshDeformer();
            properTime = Mathf.Clamp01(properTime);
            cachedProperTime = properTime > 0f ? properTime : 1f;
            InitializePhysicsStateFromTransform();
        }

        private void OnValidate()
        {
            properTime = Mathf.Clamp01(properTime);
            inertialMass = Mathf.Max(0.001f, inertialMass);
            gravitationalMass = Mathf.Max(0f, gravitationalMass);
            intrinsicSpinAxis = ResolveIntrinsicSpinAxis();
            intrinsicSpinDegPerSec = Mathf.Max(0f, intrinsicSpinDegPerSec);
            collisionSpinScale = Mathf.Max(0f, collisionSpinScale);
            collisionSpinImpulseScale = Mathf.Max(0f, collisionSpinImpulseScale);
            angularDampingPerFixedStep = Mathf.Clamp01(angularDampingPerFixedStep);
            CacheCollider();
            CacheStructuralResponse();
            CacheMeshDeformer();
            InitializePhysicsStateFromTransform();
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
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || !interpolateRenderTransform)
            {
                transform.SetPositionAndRotation(currentPhysicsPosition, currentPhysicsRotation);
                return;
            }

            float fixedDt = Mathf.Max(Time.fixedDeltaTime, PhysicsConstants.IntegrationEpsilon);
            float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / fixedDt);
            Vector3 renderPosition = Vector3.Lerp(previousPhysicsPosition, currentPhysicsPosition, alpha);
            Quaternion renderRotation = Quaternion.Slerp(previousPhysicsRotation, currentPhysicsRotation, alpha);
            transform.SetPositionAndRotation(renderPosition, renderRotation);
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

        public void SetAngularVelocity(Vector3 value)
        {
            angularVelocityDegPerSec = value;
        }

        public void ConfigureIntrinsicSpin(float spinDegPerSec, Vector3 axis)
        {
            intrinsicSpinDegPerSec = Mathf.Max(0f, spinDegPerSec);
            intrinsicSpinAxis = axis.sqrMagnitude > PhysicsConstants.IntegrationEpsilon ? axis.normalized : Vector3.up;
        }

        public void SetPhysicsState(Vector3 position, Quaternion rotation, Vector4 nextFourVelocity)
        {
            previousPhysicsPosition = currentPhysicsPosition;
            previousPhysicsRotation = currentPhysicsRotation;

            currentPhysicsPosition = position;
            currentPhysicsRotation = rotation;

            transform.SetPositionAndRotation(position, rotation);
            sphericalPosition = CartesianToSpherical(position);
            fourVelocity = nextFourVelocity;
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

        private void CacheCollider()
        {
            if (cachedCollider == null)
            {
                cachedCollider = GetComponent<Collider>();
            }

            if (cachedCollider == null)
            {
                cachedCollider = GetComponentInChildren<Collider>(true);
            }
        }

        private void CacheStructuralResponse()
        {
            if (cachedStructuralResponse == null)
            {
                cachedStructuralResponse = GetComponent<StructuralResponseBody>();
            }
        }

        private void CacheMeshDeformer()
        {
            if (cachedMeshDeformer == null)
            {
                cachedMeshDeformer = GetComponent<MeshNodeDeformer>();
            }
        }

        private void InitializePhysicsStateFromTransform()
        {
            previousPhysicsPosition = transform.position;
            currentPhysicsPosition = transform.position;
            previousPhysicsRotation = transform.rotation;
            currentPhysicsRotation = transform.rotation;
        }

        private Vector3 ResolveIntrinsicSpinAxis()
        {
            return intrinsicSpinAxis.sqrMagnitude > PhysicsConstants.IntegrationEpsilon ? intrinsicSpinAxis.normalized : Vector3.up;
        }

        private static Vector3 CartesianToSpherical(Vector3 p)
        {
            float r = p.magnitude;
            if (r <= PhysicsConstants.IntegrationEpsilon)
            {
                return Vector3.zero;
            }

            float theta = Mathf.Acos(Mathf.Clamp(p.y / r, -1f, 1f));
            float phi = Mathf.Atan2(p.z, p.x);
            return new Vector3(r, theta, phi);
        }
    }
}
