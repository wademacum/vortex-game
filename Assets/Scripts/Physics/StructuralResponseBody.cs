using System;
using UnityEngine;

namespace Vortex.Physics
{
    [DisallowMultipleComponent]
    public sealed class StructuralResponseBody : MonoBehaviour
    {
        [Header("Structural Limits")]
        [SerializeField, Min(0f)] private float compressionYield = 8f;
        [SerializeField, Min(0f)] private float tensionYield = 6f;
        [SerializeField, Min(0f)] private float fractureThreshold = 18f;
        [SerializeField, Min(0f)] private float collapseThreshold = 24f;

        [Header("Core Pressure")]
        [SerializeField, Min(0f)] private float corePressureSupport = 10f;
        [SerializeField, Min(0f)] private float stressRelaxPerSecond = 4f;
        [SerializeField, Min(0f)] private float collapseRisePerSecond = 1.5f;
        [SerializeField, Min(0f)] private float collapseRecoverPerSecond = 0.8f;

        [Header("Stellar")]
        [SerializeField] private bool canTriggerNova;
        [SerializeField, Min(0f)] private float novaCollapseThreshold = 1f;

        [Header("Runtime")]
        [SerializeField, Min(0f)] private float compressionStress;
        [SerializeField, Min(0f)] private float tensionStress;
        [SerializeField, Range(0f, 1f)] private float collapseProgress;
        [SerializeField] private bool fractured;
        [SerializeField] private bool novaTriggered;

        public event Action<StructuralResponseBody> FractureTriggered;
        public event Action<StructuralResponseBody> NovaTriggered;

        public float CompressionStress => compressionStress;
        public float TensionStress => tensionStress;
        public float CollapseProgress => collapseProgress;
        public bool IsFractured => fractured;
        public bool IsNovaTriggered => novaTriggered;

        private void OnValidate()
        {
            compressionYield = Mathf.Max(0f, compressionYield);
            tensionYield = Mathf.Max(0f, tensionYield);
            fractureThreshold = Mathf.Max(0f, fractureThreshold);
            collapseThreshold = Mathf.Max(0f, collapseThreshold);
            corePressureSupport = Mathf.Max(0f, corePressureSupport);
            stressRelaxPerSecond = Mathf.Max(0f, stressRelaxPerSecond);
            collapseRisePerSecond = Mathf.Max(0f, collapseRisePerSecond);
            collapseRecoverPerSecond = Mathf.Max(0f, collapseRecoverPerSecond);
            novaCollapseThreshold = Mathf.Max(0f, novaCollapseThreshold);
            compressionStress = Mathf.Max(0f, compressionStress);
            tensionStress = Mathf.Max(0f, tensionStress);
            collapseProgress = Mathf.Clamp01(collapseProgress);
        }

        public void ApplyContactStress(float penetrationDepth, float closingSpeed, float deltaTime)
        {
            float dt = Mathf.Max(0f, deltaTime);
            float depth = Mathf.Max(0f, penetrationDepth);
            float speed = Mathf.Max(0f, closingSpeed);
            if (dt <= 0f)
            {
                return;
            }

            float compressiveImpulse = depth * (0.5f + speed);
            compressionStress += compressiveImpulse;
        }

        public void ApplyTidalStress(float tidalAcceleration, float deltaTime)
        {
            float dt = Mathf.Max(0f, deltaTime);
            if (dt <= 0f)
            {
                return;
            }

            float stress = Mathf.Abs(tidalAcceleration) * dt;
            tensionStress += stress;
        }

        public void StepSimulation(float deltaTime)
        {
            float dt = Mathf.Max(0f, deltaTime);
            if (dt <= 0f)
            {
                return;
            }

            float support = corePressureSupport * dt;
            if (compressionStress > support)
            {
                float overload = compressionStress - support;
                collapseProgress += overload * (collapseRisePerSecond / Mathf.Max(0.001f, collapseThreshold)) * dt;
            }
            else
            {
                collapseProgress -= collapseRecoverPerSecond * dt;
            }

            collapseProgress = Mathf.Clamp01(collapseProgress);

            float relax = stressRelaxPerSecond * dt;
            compressionStress = Mathf.Max(0f, compressionStress - relax);
            tensionStress = Mathf.Max(0f, tensionStress - relax);

            if (!fractured)
            {
                float fractureLoad = (compressionStress / Mathf.Max(0.001f, compressionYield)) + (tensionStress / Mathf.Max(0.001f, tensionYield));
                if (fractureLoad >= fractureThreshold)
                {
                    fractured = true;
                    FractureTriggered?.Invoke(this);
                }
            }

            if (!novaTriggered && canTriggerNova)
            {
                if (collapseProgress >= novaCollapseThreshold)
                {
                    novaTriggered = true;
                    NovaTriggered?.Invoke(this);
                }
            }
        }

        public void ConfigureFromRuntimeData(
            float configuredCorePressure,
            float configuredFractureThreshold,
            float configuredCollapseThreshold,
            float configuredNovaThreshold,
            float structuralDamping,
            bool allowNova)
        {
            corePressureSupport = Mathf.Max(0f, configuredCorePressure);
            fractureThreshold = Mathf.Max(0f, configuredFractureThreshold);
            collapseThreshold = Mathf.Max(0f, configuredCollapseThreshold);
            novaCollapseThreshold = Mathf.Max(0f, configuredNovaThreshold);
            stressRelaxPerSecond = Mathf.Max(0f, structuralDamping);
            canTriggerNova = allowNova;
        }
    }
}
