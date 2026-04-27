using UnityEngine;

namespace Vortex.Procedural
{
    public abstract class CelestialBodyTemplate : ScriptableObject
    {
        [Header("Identity")]
        public BodyClass bodyClass;

        [Min(0f)]
        public float spawnWeight = 1f;

        [Header("Physical Ranges")]
        public Vector2 massRange = new Vector2(1000f, 100000f);
        public Vector2 radiusRange = new Vector2(50f, 150f);
        public Vector2 densityRange = new Vector2(1f, 10f);
        public Vector2 rotationRange = new Vector2(0f, 10f);
        public Vector2 temperatureRange = new Vector2(0f, 1000f);
        public Vector2 albedoRange = new Vector2(0.1f, 0.9f);

        [Header("Generation")]
        public GenerationMode generationMode = GenerationMode.SolidSdf;
        public NoiseLayerConfig noiseLayerConfig;

        [Header("Gameplay")]
        public bool hasSurface = true;
        public bool hasAtmosphere;
        public bool hasEventHorizon;
        public bool supportsLanding = true;
        public bool radiationHazard;

        [Range(0f, 1f)]
        public float anomalyChance = 0.05f;

        [Header("Structural Simulation")]
        [Min(0f)] public float corePressureSupport = 10f;
        [Min(0f)] public float fractureThreshold = 18f;
        [Min(0f)] public float collapseThreshold = 24f;
        [Min(0f)] public float novaThreshold = 1f;
        [Min(0f)] public float structuralDamping = 4f;

        [Header("Mesh Deformation")]
        public bool enableMeshNodeDeformation = true;
        [Min(0f)] public float meshTidalStartThreshold = 0.02f;
        [Min(0f)] public float meshTidalMaxThreshold = 2.0f;
        [Min(0f)] public float meshAxialStretchAtFull = 1.6f;
        [Min(0f)] public float meshRadialSqueezeAtFull = 0.55f;

        [Header("Rendering")]
        public Gradient[] biomeColorCurves;
        public Vector2 emissiveRange = new Vector2(0f, 1f);

        [SerializeField, HideInInspector] private int changeVersion;
        public int ChangeVersion => changeVersion;

        protected void NotifyTemplateChanged()
        {
            unchecked
            {
                changeVersion++;
            }
        }

        protected void EnsureSolidSdfNoiseDefaults()
        {
            if ((generationMode & GenerationMode.SolidSdf) == 0)
            {
                return;
            }

            bool allZero =
                Mathf.Approximately(noiseLayerConfig.continent.amplitude, 0f) &&
                Mathf.Approximately(noiseLayerConfig.mountain.amplitude, 0f) &&
                Mathf.Approximately(noiseLayerConfig.detail.amplitude, 0f);

            if (!allZero)
            {
                return;
            }

            noiseLayerConfig.continent = new NoiseLayer
            {
                scale = 0.01f,
                octaves = 4,
                amplitude = 20f,
                persistence = 0.5f,
                lacunarity = 2f,
                offset = new Vector3(17f, 31f, 53f)
            };

            noiseLayerConfig.mountain = new NoiseLayer
            {
                scale = 0.03f,
                octaves = 4,
                amplitude = 9f,
                persistence = 0.55f,
                lacunarity = 2f,
                offset = new Vector3(113f, 79f, 41f)
            };

            noiseLayerConfig.detail = new NoiseLayer
            {
                scale = 0.08f,
                octaves = 3,
                amplitude = 2f,
                persistence = 0.5f,
                lacunarity = 2f,
                offset = new Vector3(199f, 157f, 89f)
            };
        }
    }
}
