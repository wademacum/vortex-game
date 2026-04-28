using UnityEngine;

namespace Vortex.Procedural
{
    [CreateAssetMenu(fileName = "PlanetTemplate", menuName = "Vortex/CelestialBody/Planet")]
    public sealed class PlanetTemplate : CelestialBodyTemplate
    {
        [Header("Planet Shape")]
        public BaseShapeConfig baseShapeConfig;
        public PlanetShapeConfig planetShapeConfig;

        [Header("Planet Shading")]
        public PlanetShadingConfig planetShadingConfig;

        private void OnValidate()
        {
            bodyClass = BodyClass.Planet;
            generationMode |= GenerationMode.SolidSdf;
            EnsureSolidSdfNoiseDefaults();
            EnsurePlanetDefaults();
            NotifyTemplateChanged();
        }

        private void Reset()
        {
            bodyClass = BodyClass.Planet;
            generationMode = GenerationMode.SolidSdf;
            hasSurface = true;
            supportsLanding = true;
            EnsureSolidSdfNoiseDefaults();
            EnsurePlanetDefaults();
            NotifyTemplateChanged();
        }

        [ContextMenu("Randomize Planet Shape")]
        private void RandomizePlanetShapeContext()
        {
            CelestialBodyTemplate.Randomize(this, System.Environment.TickCount);
            NotifyTemplateChanged();
        }

        private void EnsurePlanetDefaults()
        {
            baseShapeConfig.commonOffset = Vector3.ClampMagnitude(baseShapeConfig.commonOffset, 8f);
            baseShapeConfig.radiusBias = Mathf.Clamp(baseShapeConfig.radiusBias, -0.12f, 0.12f);

            if (baseShapeConfig.verticalSquash <= 0f)
            {
                baseShapeConfig.verticalSquash = 1f;
            }

            if (planetShapeConfig.continent.amplitude <= 0f)
            {
                planetShapeConfig.continent = noiseLayerConfig.continent;
            }

            if (planetShapeConfig.mountain.amplitude <= 0f)
            {
                planetShapeConfig.mountain = noiseLayerConfig.mountain;
            }

            if (planetShapeConfig.detail.amplitude <= 0f)
            {
                planetShapeConfig.detail = noiseLayerConfig.detail;
            }

            if (planetShapeConfig.mask.scale <= 0f)
            {
                planetShapeConfig.mask = new NoiseLayer
                {
                    scale = 0.015f,
                    octaves = 3,
                    amplitude = 1f,
                    persistence = 0.5f,
                    lacunarity = 2f,
                    offset = new Vector3(67f, 23f, 149f)
                };
            }

            planetShapeConfig.continentFloor = Mathf.Max(planetShapeConfig.continentFloor, 0.15f);
            planetShapeConfig.continentStrength = Mathf.Max(planetShapeConfig.continentStrength, 1.2f);
            planetShapeConfig.mountainStrength = Mathf.Max(planetShapeConfig.mountainStrength, 0.85f);
            planetShapeConfig.detailStrength = Mathf.Max(planetShapeConfig.detailStrength, 0.35f);
            planetShapeConfig.oceanDepthMultiplier = Mathf.Max(planetShapeConfig.oceanDepthMultiplier, 2f);
            planetShapeConfig.oceanFloorDepth = Mathf.Max(planetShapeConfig.oceanFloorDepth, 1f);
            planetShapeConfig.oceanFloorSmoothing = Mathf.Max(planetShapeConfig.oceanFloorSmoothing, 0.5f);
            planetShapeConfig.mountainBlend = Mathf.Max(planetShapeConfig.mountainBlend, 1f);

            EnsurePlanetShadingDefaults();
            EnsurePlanetRanges();
            EnsurePlanetGradients();
        }

        private void EnsurePlanetShadingDefaults()
        {
            if (planetShadingConfig.largeNoise.scale <= 0f)
            {
                planetShadingConfig.largeNoise = new NoiseLayer
                {
                    scale = 0.008f,
                    octaves = 3,
                    amplitude = 1f,
                    persistence = 0.5f,
                    lacunarity = 2f,
                    offset = new Vector3(11f, 29f, 47f)
                };
            }

            if (planetShadingConfig.smallNoise.scale <= 0f)
            {
                planetShadingConfig.smallNoise = new NoiseLayer
                {
                    scale = 0.03f,
                    octaves = 3,
                    amplitude = 1f,
                    persistence = 0.5f,
                    lacunarity = 2f,
                    offset = new Vector3(89f, 107f, 131f)
                };
            }

            if (planetShadingConfig.detailNoise.scale <= 0f)
            {
                planetShadingConfig.detailNoise = new NoiseLayer
                {
                    scale = 0.07f,
                    octaves = 2,
                    amplitude = 1f,
                    persistence = 0.5f,
                    lacunarity = 2f,
                    offset = new Vector3(151f, 173f, 197f)
                };
            }

            if (planetShadingConfig.detailWarpNoise.scale <= 0f)
            {
                planetShadingConfig.detailWarpNoise = new NoiseLayer
                {
                    scale = 0.02f,
                    octaves = 2,
                    amplitude = 1f,
                    persistence = 0.5f,
                    lacunarity = 2f,
                    offset = new Vector3(211f, 223f, 239f)
                };
            }
        }

        private void EnsurePlanetRanges()
        {
            if (massRange == Vector2.zero) massRange = new Vector2(90000f, 180000f);
            if (radiusRange == Vector2.zero) radiusRange = new Vector2(200f, 320f);
            if (densityRange == Vector2.zero) densityRange = new Vector2(2.5f, 6.5f);
            if (rotationRange == Vector2.zero) rotationRange = new Vector2(0.2f, 3.5f);
            if (temperatureRange == Vector2.zero) temperatureRange = new Vector2(180f, 420f);
            if (albedoRange == Vector2.zero) albedoRange = new Vector2(0.2f, 0.65f);
        }

        private void EnsurePlanetGradients()
        {
            if (biomeColorCurves != null && biomeColorCurves.Length > 0)
            {
                return;
            }

            biomeColorCurves = new[]
            {
                CreateGradient(new Color(0.82f, 0.76f, 0.53f), new Color(0.27f, 0.45f, 0.19f)),
                CreateGradient(new Color(0.27f, 0.45f, 0.19f), new Color(0.44f, 0.39f, 0.31f)),
                CreateGradient(new Color(0.44f, 0.39f, 0.31f), new Color(0.93f, 0.93f, 0.93f))
            };
        }

        public void ApplyAuthoringDefaults()
        {
            EnsureSolidSdfNoiseDefaults();
            EnsurePlanetDefaults();
            NotifyTemplateChanged();
        }
    }
}
