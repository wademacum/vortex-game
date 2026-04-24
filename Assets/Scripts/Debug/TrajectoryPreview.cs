using System.Collections.Generic;
using UnityEngine;
using Vortex.Physics;

namespace Vortex.Debugging
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class TrajectoryPreview : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private RelativisticBody targetBody;
        [SerializeField] private bool useBodyFourVelocity = true;
        [SerializeField] private Vector3 manualInitialVelocity = new Vector3(0f, 0f, 21f);

        [Header("Preview")]
        [SerializeField] private bool previewInEditMode = true;
        [SerializeField, Min(10)] private int simulationSteps = 600;
        [SerializeField, Min(0.001f)] private float simulationTimeStep = 0.02f;
        [SerializeField, Min(0.01f)] private float minPointDistance = 0.1f;

        [Header("Line Style")]
        [SerializeField, Min(0.001f)] private float lineWidth = 0.35f;
        [SerializeField] private Color lineStartColor = new Color(0.3f, 1f, 0.95f, 0.95f);
        [SerializeField] private Color lineEndColor = new Color(1f, 0.85f, 0.3f, 0.95f);

        private readonly List<Vector3> points = new List<Vector3>(1024);
        private readonly List<GravityWell> cachedWells = new List<GravityWell>(32);

        private LineRenderer lineRenderer;
        private Vector3 lastKnownPosition;
        private Vector4 lastKnownFourVelocity;
        private Vector3 lastKnownManualVelocity;
        private bool lastKnownUseBodyFourVelocity;

        private void Awake()
        {
            SetupLineRenderer();
            TryAutoAssignBody();
            InitializeTracking();
            RebuildPreview();
        }

        private void OnEnable()
        {
            SetupLineRenderer();
            TryAutoAssignBody();
            InitializeTracking();
            RebuildPreview();
        }

        private void OnDisable()
        {
        }

        private void OnDrawGizmosSelected()
        {
            #if UNITY_EDITOR
            if (Application.isPlaying)
            {
                return; // Let Update() handle it in Play mode
            }

            if (!previewInEditMode)
            {
                return;
            }

            TryAutoAssignBody();

            bool hasChanged = HasPositionOrVelocityChanged();
            
            if (hasChanged)
            {
                UpdateTracking();
                RebuildPreview();
            }
            #endif
        }

        public void OnValidateFromBody()
        {
            if (Application.isPlaying)
            {
                return;
            }

            InitializeTracking();
            RebuildPreview();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            SetupLineRenderer();
            InitializeTracking();
            RebuildPreview();
        }

        private void Update()
        {
            // Play mode only
            if (!previewInEditMode)
            {
                return;
            }

            TryAutoAssignBody();

            bool hasChanged = HasPositionOrVelocityChanged();
            
            if (hasChanged)
            {
                UpdateTracking();
                RebuildPreview();
            }
        }

        private void InitializeTracking()
        {
            lastKnownPosition = transform.position;
            lastKnownUseBodyFourVelocity = useBodyFourVelocity;
            lastKnownManualVelocity = manualInitialVelocity;
            
            if (targetBody != null)
            {
                lastKnownFourVelocity = targetBody.FourVelocity;
            }
        }

        private void UpdateTracking()
        {
            lastKnownPosition = transform.position;
            lastKnownUseBodyFourVelocity = useBodyFourVelocity;
            lastKnownManualVelocity = manualInitialVelocity;
            
            if (targetBody != null)
            {
                lastKnownFourVelocity = targetBody.FourVelocity;
            }
        }

        private bool HasPositionOrVelocityChanged()
        {
            Vector3 currentPos = transform.position;
            bool posChanged = currentPos != lastKnownPosition;

            // Check if velocity source flag changed
            bool sourceChanged = useBodyFourVelocity != lastKnownUseBodyFourVelocity;

            // Check manual velocity if not using body velocity
            bool manualVelChanged = !useBodyFourVelocity && manualInitialVelocity != lastKnownManualVelocity;

            // Check body velocity if using body velocity
            bool bodyVelChanged = useBodyFourVelocity && targetBody != null && targetBody.FourVelocity != lastKnownFourVelocity;

            return posChanged || sourceChanged || manualVelChanged || bodyVelChanged;
        }
        public void RebuildPreview()
        {
            if (lineRenderer == null)
            {
                return;
            }

            TryAutoAssignBody();
            CacheGravityWells();

            Vector3 startPosition = transform.position;
            Vector3 startVelocity = ResolveInitialVelocity();

            points.Clear();
            points.Add(startPosition);

            Vector3 p = startPosition;
            Vector3 v = startVelocity;

            for (int i = 0; i < simulationSteps; i++)
            {
                IntegrateStepRk4(ref p, ref v, simulationTimeStep);
                ResolveSurfaceContacts(ref p, ref v);

                if (Vector3.Distance(points[points.Count - 1], p) >= minPointDistance)
                {
                    points.Add(p);
                }
            }

            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
        }

        [ContextMenu("Debug/Clear Trajectory Preview")]
        public void ClearPreview()
        {
            points.Clear();
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 0;
            }
        }

        private void SetupLineRenderer()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            lineRenderer.useWorldSpace = true;
            lineRenderer.widthMultiplier = lineWidth;
            lineRenderer.startColor = lineStartColor;
            lineRenderer.endColor = lineEndColor;
            lineRenderer.numCapVertices = 4;
            lineRenderer.numCornerVertices = 2;
            lineRenderer.positionCount = 0;
        }

        private void TryAutoAssignBody()
        {
            if (targetBody == null)
            {
                targetBody = GetComponent<RelativisticBody>();
            }
        }

        private Vector3 ResolveInitialVelocity()
        {
            if (useBodyFourVelocity && targetBody != null)
            {
                Vector4 fv = targetBody.FourVelocity;
                return new Vector3(fv.x, fv.y, fv.z);
            }

            return manualInitialVelocity;
        }

        private void CacheGravityWells()
        {
            cachedWells.Clear();
            GravityWell[] wells = FindObjectsByType<GravityWell>(FindObjectsSortMode.None);
            for (int i = 0; i < wells.Length; i++)
            {
                GravityWell well = wells[i];
                if (well != null && well.isActiveAndEnabled)
                {
                    cachedWells.Add(well);
                }
            }
        }

        private void IntegrateStepRk4(ref Vector3 position, ref Vector3 velocity, float dt)
        {
            Vector3 p0 = position;
            Vector3 v0 = velocity;

            Vector3 a1 = ComputeAcceleration(p0);
            Vector3 k1v = a1 * dt;
            Vector3 k1p = v0 * dt;

            Vector3 a2 = ComputeAcceleration(p0 + 0.5f * k1p);
            Vector3 k2v = a2 * dt;
            Vector3 k2p = (v0 + 0.5f * k1v) * dt;

            Vector3 a3 = ComputeAcceleration(p0 + 0.5f * k2p);
            Vector3 k3v = a3 * dt;
            Vector3 k3p = (v0 + 0.5f * k2v) * dt;

            Vector3 a4 = ComputeAcceleration(p0 + k3p);
            Vector3 k4v = a4 * dt;
            Vector3 k4p = (v0 + k3v) * dt;

            Vector3 nextVelocity = v0 + (k1v + 2f * k2v + 2f * k3v + k4v) / 6f;
            Vector3 nextPosition = p0 + (k1p + 2f * k2p + 2f * k3p + k4p) / 6f;

            float hardSpeed = PhysicsConstants.HardSpeedLimitRatio * PhysicsConstants.SpeedOfLight;
            if (nextVelocity.magnitude > hardSpeed && nextVelocity.sqrMagnitude > PhysicsConstants.IntegrationEpsilon)
            {
                nextVelocity = nextVelocity.normalized * hardSpeed;
            }

            position = nextPosition;
            velocity = nextVelocity;
        }

        private Vector3 ComputeAcceleration(Vector3 position)
        {
            Vector3 acceleration = Vector3.zero;

            for (int i = 0; i < cachedWells.Count; i++)
            {
                GravityWell well = cachedWells[i];
                if (well == null || well.Mass <= 0f)
                {
                    continue;
                }

                Vector3 toWell = well.transform.position - position;
                float minRadius = Mathf.Max(
                    well.PhysicalRadius,
                    well.SchwarzschildRadius + PhysicsConstants.IntegrationEpsilon,
                    PhysicsConstants.IntegrationEpsilon
                );
                float sqrMinRadius = minRadius * minRadius;
                float sqrDistance = Mathf.Max(toWell.sqrMagnitude, sqrMinRadius);
                float distance = Mathf.Sqrt(sqrDistance);
                Vector3 direction = toWell / distance;

                float a = PhysicsConstants.GravitationalConstant * well.Mass / sqrDistance;
                acceleration += direction * a;
            }

            return acceleration;
        }

        private void ResolveSurfaceContacts(ref Vector3 position, ref Vector3 velocity)
        {
            for (int i = 0; i < cachedWells.Count; i++)
            {
                GravityWell well = cachedWells[i];
                if (well == null || !well.EnableSurfaceCollision)
                {
                    continue;
                }

                float contactRadius = Mathf.Max(well.PhysicalRadius, well.SchwarzschildRadius + PhysicsConstants.IntegrationEpsilon);
                if (contactRadius <= PhysicsConstants.IntegrationEpsilon)
                {
                    continue;
                }

                Vector3 fromCenter = position - well.transform.position;
                float distance = fromCenter.magnitude;
                if (distance >= contactRadius)
                {
                    continue;
                }

                Vector3 normal = distance > PhysicsConstants.IntegrationEpsilon
                    ? fromCenter / distance
                    : Vector3.up;

                position = well.transform.position + normal * contactRadius;

                float radialSpeed = Vector3.Dot(velocity, normal);
                if (radialSpeed < 0f)
                {
                    float bounce = -radialSpeed * PhysicsConstants.SurfaceBounceFactor;
                    velocity -= radialSpeed * normal;
                    velocity += bounce * normal;

                    Vector3 tangential = velocity - Vector3.Dot(velocity, normal) * normal;
                    tangential *= PhysicsConstants.SurfaceTangentialDamping;
                    velocity = tangential + Vector3.Dot(velocity, normal) * normal;
                }
            }
        }
    }
}
