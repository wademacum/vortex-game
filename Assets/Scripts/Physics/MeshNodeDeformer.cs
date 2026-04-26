using System.Collections.Generic;
using UnityEngine;

namespace Vortex.Physics
{
    [DisallowMultipleComponent]
    public sealed class MeshNodeDeformer : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private bool includeChildren = true;
        [SerializeField] private MeshFilter[] explicitTargets;

        [Header("Spaghettification")]
        [SerializeField, Min(0f)] private float tidalStartThreshold = 0.01f;
        [SerializeField, Min(0f)] private float tidalMaxThreshold = 2.0f;
        [SerializeField, Min(0f)] private float axialStretchAtFull = 1.6f;
        [SerializeField, Min(0f)] private float radialSqueezeAtFull = 0.55f;
        [SerializeField, Min(0f)] private float applyRate = 1.5f;
        [SerializeField, Min(0f)] private float recoveryRate = 0.8f;
        [SerializeField] private bool recalculateBounds = true;
        [SerializeField] private bool recalculateNormals = false;

        [Header("Runtime")]
        [SerializeField, Range(0f, 1f)] private float currentDeformation;

        private readonly List<MeshFilter> targets = new List<MeshFilter>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Vector3[]> baseVertices = new List<Vector3[]>();
        private readonly List<Vector3[]> deformedVertices = new List<Vector3[]>();

        private void Awake()
        {
            CacheTargetsAndBakeInstances();
        }

        private void OnEnable()
        {
            if (meshes.Count == 0)
            {
                CacheTargetsAndBakeInstances();
            }
        }

        private void OnDisable()
        {
            currentDeformation = 0f;
            RestoreRestShape();
        }

        public void Configure(float startThreshold, float maxThreshold, float axialStretch, float radialSqueeze)
        {
            tidalStartThreshold = Mathf.Max(0f, startThreshold);
            tidalMaxThreshold = Mathf.Max(tidalStartThreshold + PhysicsConstants.IntegrationEpsilon, maxThreshold);
            axialStretchAtFull = Mathf.Max(0f, axialStretch);
            radialSqueezeAtFull = Mathf.Max(0f, radialSqueeze);
        }

        public void ApplyTidalField(Vector3 axisWorld, float tidalStrength, float deltaTime)
        {
            float dt = Mathf.Max(0f, deltaTime);
            if (dt <= 0f || meshes.Count == 0)
            {
                return;
            }

            float targetDeform = ResolveTargetDeformation(tidalStrength);
            float rate = targetDeform > currentDeformation ? applyRate : recoveryRate;
            currentDeformation = Mathf.MoveTowards(currentDeformation, targetDeform, rate * dt);

            if (currentDeformation <= PhysicsConstants.IntegrationEpsilon)
            {
                RestoreRestShape();
                return;
            }

            Vector3 axis = axisWorld.sqrMagnitude > PhysicsConstants.IntegrationEpsilon
                ? axisWorld.normalized
                : Vector3.up;

            Vector3 center = transform.position;
            float axialScale = 1f + currentDeformation * axialStretchAtFull;
            float lateralScale = Mathf.Max(0.05f, 1f - currentDeformation * radialSqueezeAtFull);

            for (int i = 0; i < meshes.Count; i++)
            {
                MeshFilter filter = targets[i];
                Mesh mesh = meshes[i];
                Vector3[] source = baseVertices[i];
                Vector3[] dest = deformedVertices[i];

                Matrix4x4 localToWorld = filter.transform.localToWorldMatrix;
                Matrix4x4 worldToLocal = filter.transform.worldToLocalMatrix;

                for (int v = 0; v < source.Length; v++)
                {
                    Vector3 world = localToWorld.MultiplyPoint3x4(source[v]);
                    Vector3 rel = world - center;

                    float axial = Vector3.Dot(rel, axis);
                    Vector3 lateral = rel - axis * axial;

                    Vector3 deformedRel = axis * (axial * axialScale) + lateral * lateralScale;
                    Vector3 deformedWorld = center + deformedRel;
                    dest[v] = worldToLocal.MultiplyPoint3x4(deformedWorld);
                }

                mesh.vertices = dest;
                if (recalculateBounds)
                {
                    mesh.RecalculateBounds();
                }

                if (recalculateNormals)
                {
                    mesh.RecalculateNormals();
                }
            }
        }

        private float ResolveTargetDeformation(float tidalStrength)
        {
            float strength = Mathf.Max(0f, tidalStrength);
            if (strength <= tidalStartThreshold)
            {
                return 0f;
            }

            float maxRange = Mathf.Max(tidalStartThreshold + PhysicsConstants.IntegrationEpsilon, tidalMaxThreshold);
            float t = Mathf.InverseLerp(tidalStartThreshold, maxRange, strength);
            return Mathf.Clamp01(t);
        }

        private void RestoreRestShape()
        {
            for (int i = 0; i < meshes.Count; i++)
            {
                Mesh mesh = meshes[i];
                mesh.vertices = baseVertices[i];
                if (recalculateBounds)
                {
                    mesh.RecalculateBounds();
                }

                if (recalculateNormals)
                {
                    mesh.RecalculateNormals();
                }
            }
        }

        private void CacheTargetsAndBakeInstances()
        {
            targets.Clear();
            meshes.Clear();
            baseVertices.Clear();
            deformedVertices.Clear();

            if (explicitTargets != null && explicitTargets.Length > 0)
            {
                for (int i = 0; i < explicitTargets.Length; i++)
                {
                    TryAddTarget(explicitTargets[i]);
                }
            }
            else if (includeChildren)
            {
                MeshFilter[] found = GetComponentsInChildren<MeshFilter>(true);
                for (int i = 0; i < found.Length; i++)
                {
                    TryAddTarget(found[i]);
                }
            }
            else
            {
                TryAddTarget(GetComponent<MeshFilter>());
            }
        }

        private void TryAddTarget(MeshFilter filter)
        {
            if (filter == null || filter.sharedMesh == null)
            {
                return;
            }

            if (targets.Contains(filter))
            {
                return;
            }

            Mesh instance = Instantiate(filter.sharedMesh);
            instance.name = filter.sharedMesh.name + "_DeformedInstance";
            filter.sharedMesh = instance;

            targets.Add(filter);
            meshes.Add(instance);

            Vector3[] vertices = instance.vertices;
            Vector3[] baseCopy = new Vector3[vertices.Length];
            Vector3[] deformCopy = new Vector3[vertices.Length];
            System.Array.Copy(vertices, baseCopy, vertices.Length);
            System.Array.Copy(vertices, deformCopy, vertices.Length);

            baseVertices.Add(baseCopy);
            deformedVertices.Add(deformCopy);
        }
    }
}
