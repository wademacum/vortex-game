using UnityEngine;

namespace Vortex.Procedural
{
    [CreateAssetMenu(fileName = "MoonTemplate", menuName = "Vortex/CelestialBody/Moon")]
    public sealed class MoonTemplate : CelestialBodyTemplate
    {
        [Header("Moon Shape")]
        public BaseShapeConfig baseShapeConfig;
        public MoonShapeConfig moonShapeConfig;

        [Header("Moon Shading")]
        public MoonShadingConfig moonShadingConfig;

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

            if (moonShapeConfig.shape.amplitude <= 0f)
            {
                moonShapeConfig.shape = new NoiseLayer
                {
                    scale = 1.9f,
                    octaves = 4,
                    amplitude = 0.75f,
                    persistence = 0.5f,
                    lacunarity = 2f,
                    offset = new Vector3(13f, 31f, 59f)
                };
            }

            if (moonShapeConfig.ridgeA.amplitude <= 0f)
            {
                moonShapeConfig.ridgeA = new NoiseLayer
                {
                    scale = 1.8f,
                    octaves = 4,
                    amplitude = 1.2f,
                    persistence = 0.42f,
                    lacunarity = 2.6f,
                    offset = new Vector3(71f, 97f, 127f)
                };
            }

            if (moonShapeConfig.ridgeB.amplitude <= 0f)
            {
                moonShapeConfig.ridgeB = new NoiseLayer
                {
                    scale = 1.1f,
                    octaves = 4,
                    amplitude = 0.45f,
                    persistence = 0.6f,
                    lacunarity = 2f,
                    offset = new Vector3(149f, 181f, 211f)
                };
            }

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
                moonShapeConfig.craterDepth = 0.018f;
            }
            if (moonShapeConfig.craterRimSharpness <= 0f)
            {
                moonShapeConfig.craterRimSharpness = 1.2f;
            }
            if (moonShapeConfig.craterNoiseScale <= 0f)
            {
                moonShapeConfig.craterNoiseScale = 0.001f;
            }

            EnsureMoonShadingDefaults();
            EnsureMoonSurfaceDefaults();
            EnsureMoonRanges();
            EnsureMoonGradients();
        }

        private void EnsureMoonShadingDefaults()
        {
            if (moonShadingConfig.biomePointCount <= 0)
            {
                moonShadingConfig.biomePointCount = 18;
            }
            if (moonShadingConfig.biomeRadiusRange == Vector2.zero)
            {
                moonShadingConfig.biomeRadiusRange = new Vector2(0.08f, 0.22f);
            }

            if (moonShadingConfig.biomeWarpNoise.scale <= 0f)
            {
                moonShadingConfig.biomeWarpNoise = new NoiseLayer
                {
                    scale = 0.015f,
                    octaves = 2,
                    amplitude = 1f,
                    persistence = 0.5f,
                    lacunarity = 2f,
                    offset = new Vector3(17f, 37f, 73f)
                };
            }

            if (moonShadingConfig.detailNoise.scale <= 0f)
            {
                moonShadingConfig.detailNoise = new NoiseLayer
                {
                    scale = 0.08f,
                    octaves = 2,
                    amplitude = 1f,
                    persistence = 0.5f,
                    lacunarity = 2f,
                    offset = new Vector3(101f, 139f, 167f)
                };
            }

            if (moonShadingConfig.detailWarpNoise.scale <= 0f)
            {
                moonShadingConfig.detailWarpNoise = new NoiseLayer
                {
                    scale = 0.03f,
                    octaves = 2,
                    amplitude = 1f,
                    persistence = 0.5f,
                    lacunarity = 2f,
                    offset = new Vector3(191f, 223f, 251f)
                };
            }

            if (moonShadingConfig.candidatePoolSize <= 0f)
            {
                moonShadingConfig.candidatePoolSize = 0.1f;
            }
            if (moonShadingConfig.desiredEjectaRays <= 0)
            {
                moonShadingConfig.desiredEjectaRays = 1;
            }
            if (moonShadingConfig.ejectaRaysScale <= 0f)
            {
                moonShadingConfig.ejectaRaysScale = 1f;
            }
        }

        private void EnsureMoonSurfaceDefaults()
        {
            if (moonSurfaceConfig.mainTextureScale <= 0f) moonSurfaceConfig.mainTextureScale = 0.012f;
            if (moonSurfaceConfig.flatSurfaceScale <= 0f) moonSurfaceConfig.flatSurfaceScale = 0.03f;
            if (moonSurfaceConfig.steepSurfaceScale <= 0f) moonSurfaceConfig.steepSurfaceScale = 0.045f;
            if (moonSurfaceConfig.textureBlendStrength <= 0f) moonSurfaceConfig.textureBlendStrength = 0.28f;
            if (moonSurfaceConfig.ejectaBrightness <= 0f) moonSurfaceConfig.ejectaBrightness = 0.14f;
            if (moonSurfaceConfig.steepDarkening <= 0f) moonSurfaceConfig.steepDarkening = 0.16f;

            if (moonSurfaceConfig.mainTextureScale <= 0f) moonSurfaceConfig.mainTextureScale = 0.001f;
            if (moonSurfaceConfig.flatSurfaceScale <= 0f) moonSurfaceConfig.flatSurfaceScale = 0.001f;
            if (moonSurfaceConfig.steepSurfaceScale <= 0f) moonSurfaceConfig.steepSurfaceScale = 0.001f;

            #if UNITY_EDITOR
            TryAssignSurfaceTexture(ref moonSurfaceConfig.mainTexture, "MoonNoise");
            TryAssignSurfaceTexture(ref moonSurfaceConfig.craterRayTexture, "CraterEjectaRay");
            TryAssignSurfaceTexture(ref moonSurfaceConfig.flatSurfaceTexture, "Rock1");
            TryAssignSurfaceTexture(ref moonSurfaceConfig.steepSurfaceTexture, "SnowOld");
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
