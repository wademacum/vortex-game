using UnityEngine;

namespace Vortex.Procedural
{
    [CreateAssetMenu(fileName = "AsteroidTemplate", menuName = "Vortex/CelestialBody/Asteroid")]
    public sealed class AsteroidClusterTemplate : CelestialBodyTemplate
    {
        [Header("Base Shape")]
        public BaseShapeConfig baseShapeConfig;

        [Header("Asteroid Shape")]
        public AsteroidShapeConfig asteroidShapeConfig;

        [Header("Asteroid Shading")]
        public AsteroidShadingConfig asteroidShadingConfig;

        [Header("Moon-like Surface")]
        public MoonBiomeConfig moonBiomeConfig;
        public MoonSurfaceConfig moonSurfaceConfig;

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
            if (baseShapeConfig.verticalSquash <= 0f)
            {
                baseShapeConfig.verticalSquash = 1f;
            }

            if (asteroidShapeConfig.baseShape.amplitude <= 0f)
            {
                asteroidShapeConfig.baseShape = new NoiseLayer
                {
                    scale = 0.028f,
                    octaves = 4,
                    amplitude = 5.5f,
                    persistence = 0.5f,
                    lacunarity = 2.1f,
                    offset = new Vector3(11f, 23f, 47f)
                };
            }
            if (asteroidShapeConfig.detailA.amplitude <= 0f)
            {
                asteroidShapeConfig.detailA = new NoiseLayer
                {
                    scale = 0.06f,
                    octaves = 3,
                    amplitude = 2.8f,
                    persistence = 0.48f,
                    lacunarity = 2.25f,
                    offset = new Vector3(61f, 73f, 89f)
                };
            }
            if (asteroidShapeConfig.detailB.amplitude <= 0f)
            {
                asteroidShapeConfig.detailB = new NoiseLayer
                {
                    scale = 0.12f,
                    octaves = 2,
                    amplitude = 1.4f,
                    persistence = 0.58f,
                    lacunarity = 2.1f,
                    offset = new Vector3(101f, 113f, 131f)
                };
            }
            if (asteroidShapeConfig.pitCount <= 0)
                asteroidShapeConfig.pitCount = 42;
            if (asteroidShapeConfig.pitRadiusRange == Vector2.zero)
                asteroidShapeConfig.pitRadiusRange = new Vector2(0.02f, 0.1f);
            if (asteroidShapeConfig.pitDepth <= 0f)
                asteroidShapeConfig.pitDepth = 0.028f;
            if (asteroidShapeConfig.pitRimSharpness <= 0f)
                asteroidShapeConfig.pitRimSharpness = 0.72f;
            if (asteroidShapeConfig.surfaceIrregularity <= 0f)
                asteroidShapeConfig.surfaceIrregularity = 0.35f;

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

            if (moonBiomeConfig.biomeWarpNoise.scale <= 0f)
            {
                moonBiomeConfig.biomeWarpNoise = new NoiseLayer
                {
                    scale = 0.012f,
                    octaves = 2,
                    amplitude = 1f,
                    persistence = 0.5f,
                    lacunarity = 2f,
                    offset = new Vector3(17f, 37f, 73f)
                };
            }
            if (moonBiomeConfig.mariaBias <= 0f) moonBiomeConfig.mariaBias = 0.3f;
            if (moonBiomeConfig.highlandBias <= 0f) moonBiomeConfig.highlandBias = 0.3f;
            if (moonBiomeConfig.colorVariation <= 0f) moonBiomeConfig.colorVariation = 0.16f;

            if (moonSurfaceConfig.mainTextureScale <= 0f) moonSurfaceConfig.mainTextureScale = 0.01f;
            if (moonSurfaceConfig.flatSurfaceScale <= 0f) moonSurfaceConfig.flatSurfaceScale = 0.014f;
            if (moonSurfaceConfig.steepSurfaceScale <= 0f) moonSurfaceConfig.steepSurfaceScale = 0.02f;
            if (moonSurfaceConfig.microDetailScale <= 0f) moonSurfaceConfig.microDetailScale = 0.04f;
            if (moonSurfaceConfig.textureBlendStrength <= 0f) moonSurfaceConfig.textureBlendStrength = 0.5f;
            if (moonSurfaceConfig.normalBlendStrength <= 0f) moonSurfaceConfig.normalBlendStrength = 0.5f;
            if (moonSurfaceConfig.microDetailStrength <= 0f) moonSurfaceConfig.microDetailStrength = 0.4f;
            if (moonSurfaceConfig.flatContrast <= 0f) moonSurfaceConfig.flatContrast = 1.1f;
            if (moonSurfaceConfig.steepContrast <= 0f) moonSurfaceConfig.steepContrast = 1.08f;
            if (moonSurfaceConfig.albedoSaturation <= 0f) moonSurfaceConfig.albedoSaturation = 0.82f;
            if (moonSurfaceConfig.ejectaBrightness <= 0f) moonSurfaceConfig.ejectaBrightness = 0.1f;
            if (moonSurfaceConfig.steepDarkening <= 0f) moonSurfaceConfig.steepDarkening = 0.2f;

            noiseLayerConfig.continent = asteroidShapeConfig.baseShape;
            noiseLayerConfig.mountain = asteroidShapeConfig.detailA;
            noiseLayerConfig.detail = asteroidShapeConfig.detailB;

            if (massRange == Vector2.zero) massRange = new Vector2(180f, 2400f);
            if (radiusRange == Vector2.zero) radiusRange = new Vector2(18f, 65f);
            if (densityRange == Vector2.zero) densityRange = new Vector2(1.6f, 4.2f);
            if (rotationRange == Vector2.zero) rotationRange = new Vector2(0.08f, 2.8f);
            if (temperatureRange == Vector2.zero) temperatureRange = new Vector2(45f, 250f);
            if (albedoRange == Vector2.zero) albedoRange = new Vector2(0.05f, 0.3f);

            if (biomeColorCurves == null || biomeColorCurves.Length == 0)
            {
                biomeColorCurves = new[]
                {
                    CelestialBodyTemplate.CreateGradient(new Color(0.12f, 0.12f, 0.12f), new Color(0.42f, 0.40f, 0.38f)),
                    CelestialBodyTemplate.CreateGradient(new Color(0.42f, 0.40f, 0.38f), new Color(0.72f, 0.70f, 0.67f))
                };
            }

            #if UNITY_EDITOR
            TryAssignSurfaceTexture(ref moonSurfaceConfig.mainTexture, "MoonNoise");
            TryAssignSurfaceTexture(ref moonSurfaceConfig.craterRayTexture, "CraterEjectaRay");
            TryAssignSurfaceTexture(ref moonSurfaceConfig.flatSurfaceTexture, "Rock1");
            TryAssignSurfaceTexture(ref moonSurfaceConfig.steepSurfaceTexture, "Rock1");
            TryAssignSurfaceTexture(ref moonSurfaceConfig.flatNormalMap, "Rock1Normal");
            TryAssignSurfaceTexture(ref moonSurfaceConfig.steepNormalMap, "Rock1Normal");
            #endif
        }

        public void ApplyAuthoringDefaults()
        {
            EnsureSolidSdfNoiseDefaults();
            EnsureAsteroidDefaults();
            NotifyTemplateChanged();
        }
    }
}
