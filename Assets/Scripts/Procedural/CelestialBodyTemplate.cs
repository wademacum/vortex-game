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

        [Header("Rendering")]
        public Gradient[] biomeColorCurves;
        public Vector2 emissiveRange = new Vector2(0f, 1f);
    }
}
