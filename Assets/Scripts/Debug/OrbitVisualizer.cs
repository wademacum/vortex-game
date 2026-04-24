using System.Collections.Generic;
using UnityEngine;

namespace Vortex.Debugging
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class OrbitVisualizer : MonoBehaviour
    {
        [Header("Sampling")]
        [SerializeField, Min(0.001f)] private float sampleInterval = 0.05f;
        [SerializeField, Min(0.01f)] private float minPointDistance = 0.2f;
        [SerializeField, Min(16)] private int maxPoints = 1024;

        [Header("Line Style")]
        [SerializeField, Min(0.001f)] private float lineWidth = 0.12f;
        [SerializeField] private Color startColor = new Color(0.2f, 0.9f, 1f, 0.95f);
        [SerializeField] private Color endColor = new Color(1f, 0.5f, 0.2f, 0.95f);

        [Header("Behavior")]
        [SerializeField] private bool clearOnEnable = true;

        private readonly List<Vector3> points = new List<Vector3>();
        private LineRenderer lineRenderer;
        private float sampleTimer;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            ConfigureLineRenderer();
        }

        private void OnEnable()
        {
            if (clearOnEnable)
            {
                ClearPath();
            }
        }

        private void LateUpdate()
        {
            sampleTimer += Time.deltaTime;
            if (sampleTimer < sampleInterval)
            {
                return;
            }

            sampleTimer = 0f;
            TryAppendPoint(transform.position);
        }

        [ContextMenu("Debug/Clear Orbit Path")]
        public void ClearPath()
        {
            points.Clear();
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 0;
            }
        }

        private void TryAppendPoint(Vector3 worldPosition)
        {
            if (points.Count > 0)
            {
                float distance = Vector3.Distance(points[points.Count - 1], worldPosition);
                if (distance < minPointDistance)
                {
                    return;
                }
            }

            points.Add(worldPosition);
            if (points.Count > maxPoints)
            {
                int overflow = points.Count - maxPoints;
                points.RemoveRange(0, overflow);
            }

            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
        }

        private void ConfigureLineRenderer()
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.widthMultiplier = lineWidth;
            lineRenderer.startColor = startColor;
            lineRenderer.endColor = endColor;
            lineRenderer.numCapVertices = 4;
            lineRenderer.numCornerVertices = 2;
            lineRenderer.positionCount = 0;
        }
    }
}
