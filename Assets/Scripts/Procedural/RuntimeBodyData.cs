using UnityEngine;

namespace Vortex.Procedural
{
    public struct RuntimeBodyData
    {
        public BodyClass bodyClass;
        public GenerationMode generationMode;

        public float mass;
        public float radius;
        public float density;
        public float rotationSpeed;
        public float temperature;
        public float albedo;
        public float anomalyChance;

        public bool hasSurface;
        public bool hasAtmosphere;
        public bool hasEventHorizon;
        public bool supportsLanding;
        public bool radiationHazard;

        public NoiseLayerConfig noiseLayerConfig;
        public Gradient[] biomeColorCurves;
        public Vector2 emissiveRange;
    }
}
