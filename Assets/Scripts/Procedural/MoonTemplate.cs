using UnityEngine;

namespace Vortex.Procedural
{
    [CreateAssetMenu(fileName = "MoonTemplate", menuName = "Vortex/CelestialBody/Moon")]
    public sealed class MoonTemplate : CelestialBodyTemplate
    {
        [Header("Moon Shape")]
        public BaseShapeConfig baseShapeConfig;
        public MoonShapeConfig moonShapeConfig;
        public MoonTerrainNoiseConfig moonTerrainNoiseConfig;

        [Header("Moon Biome")]
        public MoonBiomeConfig moonBiomeConfig;

        [Header("Moon Surface")]
        public MoonSurfaceConfig moonSurfaceConfig;

        private void OnValidate()
        {
            bodyClass = BodyClass.Moon;
            generationMode |= GenerationMode.SolidSdf;
            EnsureSolidSdfNoiseDefaults();
            EnsureMoonDefaults();
            NotifyTemplateChanged();
        }

        private void Reset()
        {
            bodyClass = BodyClass.Moon;
            generationMode = GenerationMode.SolidSdf;
            hasSurface = true;
            supportsLanding = true;
            EnsureSolidSdfNoiseDefaults();
            EnsureMoonDefaults();
            NotifyTemplateChanged();
        }

        [ContextMenu("Randomize Moon Shape")]
        private void RandomizeMoonShapeContext()
        {
            CelestialBodyTemplate.Randomize(this, System.Environment.TickCount);
            NotifyTemplateChanged();
        }

        private void EnsureMoonDefaults()
        {
            if (baseShapeConfig.verticalSquash <= 0f)
            {
                baseShapeConfig.verticalSquash = 1f;
            }

            EnsureMoonTerrainDefaults();

            if (moonShapeConfig.craterCount <= 0)
            {
                moonShapeConfig.craterCount = 120;
            }
            if (moonShapeConfig.craterRadiusRange == Vector2.zero)
            {
                moonShapeConfig.craterRadiusRange = new Vector2(0.02f, 0.09f);
            }

            if (moonShapeConfig.craterRadiusRange.x <= 0f)
            {
                moonShapeConfig.craterRadiusRange.x = 0.005f;
            }
            if (moonShapeConfig.craterRadiusRange.y <= moonShapeConfig.craterRadiusRange.x)
            {
                moonShapeConfig.craterRadiusRange.y = moonShapeConfig.craterRadiusRange.x + 0.005f;
            }
            if (moonShapeConfig.craterDepth <= 0f)
            {
                moonShapeConfig.craterDepth = 0.03f;
            }
            moonShapeConfig.craterRadiusBias = Mathf.Clamp01(moonShapeConfig.craterRadiusBias <= 0f ? 0.65f : moonShapeConfig.craterRadiusBias);
            if (moonShapeConfig.craterFloorHeightRange == Vector2.zero)
            {
                moonShapeConfig.craterFloorHeightRange = new Vector2(0.68f, 0.9f);
            }
            moonShapeConfig.craterFloorRadius = Mathf.Clamp01(moonShapeConfig.craterFloorRadius <= 0f ? 0.38f : moonShapeConfig.craterFloorRadius);
            if (moonShapeConfig.craterWallSmoothness <= 0f) moonShapeConfig.craterWallSmoothness = 0.48f;
            if (moonShapeConfig.craterRimWidth <= 0f) moonShapeConfig.craterRimWidth = 0.18f;
            if (moonShapeConfig.craterRimHeight <= 0f) moonShapeConfig.craterRimHeight = 0.20f;
            if (moonShapeConfig.craterRimSharpness <= 0f)
            {
                moonShapeConfig.craterRimSharpness = 0.85f;
            }
            if (moonShapeConfig.craterEdgeWarpFrequency <= 0f) moonShapeConfig.craterEdgeWarpFrequency = 1.2f;
            if (moonShapeConfig.craterEdgeWarpStrength <= 0f) moonShapeConfig.craterEdgeWarpStrength = 0.05f;
            moonShapeConfig.craterCrowdingRadiusScale = Mathf.Clamp01(moonShapeConfig.craterCrowdingRadiusScale <= 0f ? 0.72f : moonShapeConfig.craterCrowdingRadiusScale);
            moonShapeConfig.craterDistributionJitter = Mathf.Clamp01(moonShapeConfig.craterDistributionJitter <= 0f ? 0.35f : moonShapeConfig.craterDistributionJitter);
            moonShapeConfig.youngCraterFraction = Mathf.Clamp01(moonShapeConfig.youngCraterFraction <= 0f ? 0.12f : moonShapeConfig.youngCraterFraction);

            EnsureMoonBiomeDefaults();
            EnsureMoonSurfaceDefaults();
            EnsureMoonRanges();
            EnsureMoonGradients();

            // Keep inherited noise fields in sync for shared fallback/legacy paths.
            noiseLayerConfig.continent = moonTerrainNoiseConfig.macroShape;
            noiseLayerConfig.mountain = moonTerrainNoiseConfig.ridgeNoise;
            noiseLayerConfig.detail = moonTerrainNoiseConfig.detailNoise;
        }

        private void EnsureMoonTerrainDefaults()
        {
            if (moonTerrainNoiseConfig.macroShape.amplitude <= 0f)
            {
                moonTerrainNoiseConfig.macroShape = new NoiseLayer
                {
                    scale = 0.02f,
                    octaves = 4,
                    amplitude = 6f,
                    persistence = 0.5f,
                    lacunarity = 2f,
                    offset = new Vector3(13f, 31f, 59f)
                };
            }

            if (moonTerrainNoiseConfig.ridgeNoise.amplitude <= 0f)
            {
                moonTerrainNoiseConfig.ridgeNoise = new NoiseLayer
                {
                    scale = 0.035f,
                    octaves = 4,
                    amplitude = 4f,
                    persistence = 0.45f,
                    lacunarity = 2.1f,
                    offset = new Vector3(71f, 97f, 127f)
                };
            }

            if (moonTerrainNoiseConfig.detailNoise.amplitude <= 0f)
            {
                moonTerrainNoiseConfig.detailNoise = new NoiseLayer
                {
                    scale = 0.07f,
                    octaves = 3,
                    amplitude = 2.5f,
                    persistence = 0.5f,
                    lacunarity = 2.2f,
                    offset = new Vector3(149f, 181f, 211f)
                };
            }

            if (moonTerrainNoiseConfig.warpNoise.amplitude <= 0f)
            {
                moonTerrainNoiseConfig.warpNoise = new NoiseLayer
                {
                    scale = 0.03f,
                    octaves = 2,
                    amplitude = 1f,
                    persistence = 0.5f,
                    lacunarity = 2f,
                    offset = new Vector3(191f, 223f, 251f)
                };
            }

            if (moonTerrainNoiseConfig.warpStrength <= 0f) moonTerrainNoiseConfig.warpStrength = 0.6f;
            if (moonTerrainNoiseConfig.macroStrength <= 0f) moonTerrainNoiseConfig.macroStrength = 0.14f;
            if (moonTerrainNoiseConfig.ridgeStrength <= 0f) moonTerrainNoiseConfig.ridgeStrength = 0.06f;
            if (moonTerrainNoiseConfig.detailStrength <= 0f) moonTerrainNoiseConfig.detailStrength = 0.02f;
        }

        private void EnsureMoonBiomeDefaults()
        {
            if (moonBiomeConfig.biomeWarpNoise.scale <= 0f)
            {
                moonBiomeConfig.biomeWarpNoise = new NoiseLayer
                {
                    scale = 0.015f,
                    octaves = 2,
                    amplitude = 1f,
                    persistence = 0.5f,
                    lacunarity = 2f,
                    offset = new Vector3(17f, 37f, 73f)
                };
            }

            if (moonBiomeConfig.mariaBias <= 0f) moonBiomeConfig.mariaBias = 0.35f;
            if (moonBiomeConfig.highlandBias <= 0f) moonBiomeConfig.highlandBias = 0.25f;
            if (moonBiomeConfig.colorVariation <= 0f) moonBiomeConfig.colorVariation = 0.2f;
        }

        private void EnsureMoonSurfaceDefaults()
        {
            if (moonSurfaceConfig.mainTextureScale <= 0f) moonSurfaceConfig.mainTextureScale = 0.008f;
            if (moonSurfaceConfig.flatSurfaceScale <= 0f) moonSurfaceConfig.flatSurfaceScale = 0.012f;
            if (moonSurfaceConfig.steepSurfaceScale <= 0f) moonSurfaceConfig.steepSurfaceScale = 0.018f;
            if (moonSurfaceConfig.microDetailScale <= 0f) moonSurfaceConfig.microDetailScale = 0.032f;
            if (moonSurfaceConfig.textureBlendStrength <= 0f) moonSurfaceConfig.textureBlendStrength = 0.46f;
            if (moonSurfaceConfig.normalBlendStrength <= 0f) moonSurfaceConfig.normalBlendStrength = 0.52f;
            if (moonSurfaceConfig.microDetailStrength <= 0f) moonSurfaceConfig.microDetailStrength = 0.35f;
            if (moonSurfaceConfig.flatContrast <= 0f) moonSurfaceConfig.flatContrast = 1.12f;
            if (moonSurfaceConfig.steepContrast <= 0f) moonSurfaceConfig.steepContrast = 1.06f;
            if (moonSurfaceConfig.albedoSaturation <= 0f) moonSurfaceConfig.albedoSaturation = 0.85f;
            if (moonSurfaceConfig.ejectaBrightness <= 0f) moonSurfaceConfig.ejectaBrightness = 0.14f;
            if (moonSurfaceConfig.steepDarkening <= 0f) moonSurfaceConfig.steepDarkening = 0.16f;

            if (moonSurfaceConfig.mainTextureScale <= 0f) moonSurfaceConfig.mainTextureScale = 0.001f;
            if (moonSurfaceConfig.flatSurfaceScale <= 0f) moonSurfaceConfig.flatSurfaceScale = 0.001f;
            if (moonSurfaceConfig.steepSurfaceScale <= 0f) moonSurfaceConfig.steepSurfaceScale = 0.001f;
            if (moonSurfaceConfig.microDetailScale <= 0f) moonSurfaceConfig.microDetailScale = 0.001f;

            #if UNITY_EDITOR
            TryAssignSurfaceTexture(ref moonSurfaceConfig.mainTexture, "MoonNoise");
            TryAssignSurfaceTexture(ref moonSurfaceConfig.craterRayTexture, "CraterEjectaRay");
            TryAssignSurfaceTexture(ref moonSurfaceConfig.flatSurfaceTexture, "Rock1");
            TryAssignSurfaceTexture(ref moonSurfaceConfig.steepSurfaceTexture, "Rock1");
            #endif
        }

        private void EnsureMoonRanges()
        {
            if (massRange == Vector2.zero) massRange = new Vector2(10000f, 40000f);
            if (radiusRange == Vector2.zero) radiusRange = new Vector2(80f, 170f);
            if (densityRange == Vector2.zero) densityRange = new Vector2(1.8f, 4.8f);
            if (rotationRange == Vector2.zero) rotationRange = new Vector2(0.05f, 1.2f);
            if (temperatureRange == Vector2.zero) temperatureRange = new Vector2(80f, 260f);
            if (albedoRange == Vector2.zero) albedoRange = new Vector2(0.08f, 0.45f);
        }

        private void EnsureMoonGradients()
        {
            if (biomeColorCurves != null && biomeColorCurves.Length > 0)
            {
                return;
            }

            biomeColorCurves = new[]
            {
                CreateGradient(new Color(0.14f, 0.14f, 0.16f), new Color(0.42f, 0.41f, 0.4f)),
                CreateGradient(new Color(0.42f, 0.41f, 0.4f), new Color(0.78f, 0.78f, 0.76f))
            };
        }

        public void ApplyAuthoringDefaults()
        {
            EnsureSolidSdfNoiseDefaults();
            EnsureMoonDefaults();
            NotifyTemplateChanged();
        }


    }
}
