using UnityEngine;

namespace Vortex.Procedural
{
    [CreateAssetMenu(fileName = "AsteroidClusterTemplate", menuName = "Vortex/CelestialBody/AsteroidCluster")]
    public sealed class AsteroidClusterTemplate : CelestialBodyTemplate
    {
        [Header("Asteroid Shape")]
        public AsteroidShapeConfig asteroidShapeConfig;

        [Header("Asteroid Shading")]
        public AsteroidShadingConfig asteroidShadingConfig;

        private void OnValidate()
        {
            bodyClass = BodyClass.AsteroidCluster;
            generationMode |= GenerationMode.SolidSdf;
            EnsureSolidSdfNoiseDefaults();
            EnsureAsteroidDefaults();
            NotifyTemplateChanged();
        }

        private void Reset()
        {
            bodyClass = BodyClass.AsteroidCluster;
            generationMode = GenerationMode.SolidSdf;
            hasSurface = true;
            supportsLanding = false;
            EnsureSolidSdfNoiseDefaults();
            EnsureAsteroidDefaults();
            NotifyTemplateChanged();
        }

        private void EnsureAsteroidDefaults()
        {
            if (asteroidShapeConfig.baseShape.amplitude <= 0f)
            {
                asteroidShapeConfig.baseShape = new NoiseLayer
                {
                    scale = 1.5f,
                    octaves = 3,
                    amplitude = 1.0f,
                    persistence = 0.5f,
                    lacunarity = 2.0f,
                    offset = new Vector3(11f, 23f, 47f)
                };
            }
            if (asteroidShapeConfig.detailA.amplitude <= 0f)
            {
                asteroidShapeConfig.detailA = new NoiseLayer
                {
                    scale = 2.2f,
                    octaves = 2,
                    amplitude = 0.5f,
                    persistence = 0.4f,
                    lacunarity = 2.2f,
                    offset = new Vector3(61f, 73f, 89f)
                };
            }
            if (asteroidShapeConfig.detailB.amplitude <= 0f)
            {
                asteroidShapeConfig.detailB = new NoiseLayer
                {
                    scale = 0.8f,
                    octaves = 2,
                    amplitude = 0.3f,
                    persistence = 0.6f,
                    lacunarity = 2.1f,
                    offset = new Vector3(101f, 113f, 131f)
                };
            }
            if (asteroidShapeConfig.pitCount <= 0)
                asteroidShapeConfig.pitCount = 18;
            if (asteroidShapeConfig.pitRadiusRange == Vector2.zero)
                asteroidShapeConfig.pitRadiusRange = new Vector2(0.03f, 0.12f);
            if (asteroidShapeConfig.pitDepth <= 0f)
                asteroidShapeConfig.pitDepth = 0.012f;
            if (asteroidShapeConfig.pitRimSharpness <= 0f)
                asteroidShapeConfig.pitRimSharpness = 0.9f;
            if (asteroidShapeConfig.surfaceIrregularity <= 0f)
                asteroidShapeConfig.surfaceIrregularity = 0.18f;

            if (asteroidShadingConfig.albedoSpotCount <= 0)
                asteroidShadingConfig.albedoSpotCount = 7;
            if (asteroidShadingConfig.albedoSpotRadiusRange == Vector2.zero)
                asteroidShadingConfig.albedoSpotRadiusRange = new Vector2(0.05f, 0.18f);
            if (asteroidShadingConfig.albedoSpotNoise.scale <= 0f)
            {
                asteroidShadingConfig.albedoSpotNoise = new NoiseLayer
                {
                    scale = 0.09f,
                    octaves = 2,
                    amplitude = 1f,
                    persistence = 0.5f,
                    lacunarity = 2f,
                    offset = new Vector3(17f, 37f, 73f)
                };
            }
            if (asteroidShadingConfig.surfaceDetailNoise.scale <= 0f)
            {
                asteroidShadingConfig.surfaceDetailNoise = new NoiseLayer
                {
                    scale = 0.13f,
                    octaves = 2,
                    amplitude = 1f,
                    persistence = 0.5f,
                    lacunarity = 2f,
                    offset = new Vector3(101f, 139f, 167f)
                };
            }
            if (asteroidShadingConfig.metallicVariation <= 0f)
                asteroidShadingConfig.metallicVariation = 0.08f;
            if (asteroidShadingConfig.smoothnessVariation <= 0f)
                asteroidShadingConfig.smoothnessVariation = 0.12f;
        }
    }
}
