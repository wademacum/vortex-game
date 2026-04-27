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
        [HideInInspector] public NoiseLayerConfig noiseLayerConfig;

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

        public static Gradient CreateGradient(Color a, Color b)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(a, 0f),
                    new GradientColorKey(b, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            return gradient;
        }

        #if UNITY_EDITOR
        protected static void TryAssignSurfaceTexture(ref Texture2D target, string assetName)
        {
            if (target != null)
            {
                return;
            }

            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{assetName} t:Texture2D");
            if (guids == null || guids.Length == 0)
            {
                return;
            }

            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            target = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        #endif

        public static bool Randomize(CelestialBodyTemplate template, int seed)
        {
            if (template == null)
            {
                return false;
            }

            System.Random rng = new System.Random(seed);
            if (template is PlanetTemplate planet)
            {
                RandomizePlanet(planet, rng);
                return true;
            }

            if (template is MoonTemplate moon)
            {
                RandomizeMoon(moon, rng);
                return true;
            }

            /*
            if (template is AsteroidClusterTemplate asteroid)
            {
                RandomizeAsteroid(asteroid, rng);
                return true;
            }
            */

            return false;
        }

        private static void RandomizePlanet(PlanetTemplate template, System.Random rng)
        {
            template.baseShapeConfig.commonOffset = RandomVector3(rng, -4f, 4f);
            template.baseShapeConfig.radiusBias = RandomRange(rng, -0.04f, 0.06f);
            template.baseShapeConfig.verticalSquash = RandomRange(rng, 0.95f, 1.05f);

            template.planetShapeConfig.continent = CreateNoise(rng, 0.004f, 0.014f, 10f, 32f, 3, 5);
            template.planetShapeConfig.mountain = CreateNoise(rng, 0.015f, 0.045f, 4f, 16f, 3, 5, ridgeLike: true);
            template.planetShapeConfig.detail = CreateNoise(rng, 0.05f, 0.12f, 1f, 5f, 2, 4);
            template.planetShapeConfig.mask = CreateNoise(rng, 0.008f, 0.03f, 0.7f, 1.4f, 2, 4);
            template.planetShapeConfig.oceanDepthMultiplier = RandomRange(rng, 1.6f, 5f);
            template.planetShapeConfig.oceanFloorDepth = RandomRange(rng, 0.2f, 1.8f);
            template.planetShapeConfig.oceanFloorSmoothing = RandomRange(rng, 0.25f, 1.2f);
            template.planetShapeConfig.mountainBlend = RandomRange(rng, 0.4f, 2f);

            template.planetShadingConfig.largeNoise = CreateNoise(rng, 0.004f, 0.012f, 0.5f, 1.2f, 2, 3);
            template.planetShadingConfig.smallNoise = CreateNoise(rng, 0.015f, 0.04f, 0.5f, 1.2f, 2, 4);
            template.planetShadingConfig.detailNoise = CreateNoise(rng, 0.04f, 0.1f, 0.5f, 1.2f, 2, 3);
            template.planetShadingConfig.detailWarpNoise = CreateNoise(rng, 0.01f, 0.03f, 0.3f, 1f, 2, 3);

            template.noiseLayerConfig.continent = template.planetShapeConfig.continent;
            template.noiseLayerConfig.mountain = template.planetShapeConfig.mountain;
            template.noiseLayerConfig.detail = template.planetShapeConfig.detail;

            EnsurePlanetRanges(template, rng);
            EnsurePlanetGradients(template, rng);
        }

        private static void RandomizeMoon(MoonTemplate template, System.Random rng)
        {
            template.baseShapeConfig.commonOffset = RandomVector3(rng, -2f, 2f);
            template.baseShapeConfig.radiusBias = RandomRange(rng, -0.03f, 0.04f);
            template.baseShapeConfig.verticalSquash = RandomRange(rng, 0.985f, 1.015f);

            bool minorDeformation = rng.NextDouble() <= 0.8;
            if (minorDeformation)
            {
                template.moonShapeConfig.shape = CreateNoise(rng, 1.5f, 2.5f, 0.35f, 2.5f, 4, 4);
            }
            else
            {
                template.moonShapeConfig.shape = CreateNoise(rng, 0.35f, 1.05f, 2.5f, 6f, 4, 4);
            }

            template.moonShapeConfig.ridgeA = CreateNoise(rng, 1f, 2.4f, 0.45f, 2.2f, 4, 4, ridgeLike: true);
            template.moonShapeConfig.ridgeB = CreateNoise(rng, 0.5f, 1.4f, 0.2f, 0.8f, 4, 4);

            double craterMode = rng.NextDouble();
            if (craterMode <= 0.7)
            {
                template.moonShapeConfig.craterCount = RandomRangeInt(rng, 90, 220);
                template.moonShapeConfig.craterRadiusRange = new Vector2(0.015f, 0.085f);
            }
            else if (craterMode <= 0.85)
            {
                template.moonShapeConfig.craterCount = RandomRangeInt(rng, 220, 360);
                template.moonShapeConfig.craterRadiusRange = new Vector2(0.012f, 0.07f);
            }
            else
            {
                template.moonShapeConfig.craterCount = RandomRangeInt(rng, 36, 90);
                template.moonShapeConfig.craterRadiusRange = new Vector2(0.02f, 0.16f);
            }

            template.moonShapeConfig.craterDepth = RandomRange(rng, 0.018f, 0.05f);
            template.moonShapeConfig.craterRimSharpness = RandomRange(rng, 1.4f, 2.8f);
            template.moonShapeConfig.craterNoiseScale = RandomRange(rng, 0.9f, 2.4f);

            template.moonShadingConfig.biomePointCount = RandomRangeInt(rng, 10, 42);
            template.moonShadingConfig.biomeRadiusRange = new Vector2(RandomRange(rng, 0.04f, 0.1f), RandomRange(rng, 0.12f, 0.28f));
            template.moonShadingConfig.biomeWarpNoise = CreateNoise(rng, 0.008f, 0.025f, 0.5f, 1.3f, 2, 3);
            template.moonShadingConfig.detailNoise = CreateNoise(rng, 0.04f, 0.11f, 0.5f, 1.2f, 2, 3);
            template.moonShadingConfig.detailWarpNoise = CreateNoise(rng, 0.015f, 0.04f, 0.4f, 1.2f, 2, 3);
            template.moonShadingConfig.candidatePoolSize = RandomRange(rng, 0.15f, 0.45f);
            template.moonShadingConfig.desiredEjectaRays = RandomRangeInt(rng, 1, 4);
            template.moonShadingConfig.ejectaRaysScale = RandomRange(rng, 6f, 16f);

            template.moonSurfaceConfig.mainTextureScale = RandomRange(rng, 0.006f, 0.02f);
            template.moonSurfaceConfig.flatSurfaceScale = RandomRange(rng, 0.018f, 0.045f);
            template.moonSurfaceConfig.steepSurfaceScale = RandomRange(rng, 0.025f, 0.065f);
            template.moonSurfaceConfig.textureBlendStrength = RandomRange(rng, 0.18f, 0.42f);
            template.moonSurfaceConfig.ejectaBrightness = RandomRange(rng, 0.08f, 0.25f);
            template.moonSurfaceConfig.steepDarkening = RandomRange(rng, 0.08f, 0.24f);

            template.noiseLayerConfig.continent = template.moonShapeConfig.shape;
            template.noiseLayerConfig.mountain = template.moonShapeConfig.ridgeA;
            template.noiseLayerConfig.detail = template.moonShapeConfig.ridgeB;

            EnsureMoonRanges(template, rng);
            EnsureMoonGradients(template, rng);
        }

        

        /*
        private static void EnsureAsteroidRanges(AsteroidClusterTemplate template, System.Random rng)
        {
            if (template.massRange == Vector2.zero)
            {
                float minMass = RandomRange(rng, 180f, 800f);
                float maxMass = RandomRange(rng, 900f, 2400f);
                template.massRange = new Vector2(minMass, maxMass);
            }
            if (template.radiusRange == Vector2.zero)
            {
                template.radiusRange = new Vector2(RandomRange(rng, 6f, 12f), RandomRange(rng, 14f, 28f));
            }
            if (template.densityRange == Vector2.zero)
            {
                template.densityRange = new Vector2(2.0f, 4.8f);
            }
            if (template.rotationRange == Vector2.zero)
            {
                template.rotationRange = new Vector2(0.05f, 2.2f);
            }
            if (template.temperatureRange == Vector2.zero)
            {
                template.temperatureRange = new Vector2(RandomRange(rng, 40f, 120f), RandomRange(rng, 120f, 250f));
            }
            if (template.albedoRange == Vector2.zero)
            {
                template.albedoRange = new Vector2(0.05f, 0.35f);
            }
        }
        */

        /*
        private static void EnsureAsteroidGradients(AsteroidClusterTemplate template, System.Random rng)
        {
            Color dark = Color.HSVToRGB(RandomRange(rng, 0f, 0.1f), RandomRange(rng, 0f, 0.2f), RandomRange(rng, 0.1f, 0.25f));
            Color mid = Color.HSVToRGB(RandomRange(rng, 0f, 0.15f), RandomRange(rng, 0f, 0.3f), RandomRange(rng, 0.18f, 0.45f));
            Color bright = Color.HSVToRGB(RandomRange(rng, 0f, 0.12f), RandomRange(rng, 0f, 0.2f), RandomRange(rng, 0.35f, 0.65f));
            template.biomeColorCurves = new[]
            {
                CelestialBodyTemplate.CreateGradient(dark, mid),
                CelestialBodyTemplate.CreateGradient(mid, bright)
            };
        }
        */

        private static void EnsurePlanetRanges(PlanetTemplate template, System.Random rng)
        {
            template.massRange = new Vector2(RandomRange(rng, 70000f, 100000f), RandomRange(rng, 120000f, 220000f));
            template.radiusRange = new Vector2(RandomRange(rng, 180f, 230f), RandomRange(rng, 240f, 340f));
            template.densityRange = new Vector2(2.5f, 6.5f);
            template.rotationRange = new Vector2(0.2f, 3.5f);
            template.temperatureRange = new Vector2(RandomRange(rng, 180f, 260f), RandomRange(rng, 320f, 520f));
            template.albedoRange = new Vector2(0.2f, 0.65f);
        }

        private static void EnsureMoonRanges(MoonTemplate template, System.Random rng)
        {
            template.massRange = new Vector2(RandomRange(rng, 8000f, 18000f), RandomRange(rng, 22000f, 55000f));
            template.radiusRange = new Vector2(RandomRange(rng, 70f, 110f), RandomRange(rng, 120f, 190f));
            template.densityRange = new Vector2(1.8f, 4.8f);
            template.rotationRange = new Vector2(0.05f, 1.2f);
            template.temperatureRange = new Vector2(RandomRange(rng, 80f, 150f), RandomRange(rng, 180f, 320f));
            template.albedoRange = new Vector2(0.08f, 0.45f);
        }

        private static void EnsurePlanetGradients(PlanetTemplate template, System.Random rng)
        {
            Color shore = Color.HSVToRGB((float)rng.NextDouble(), RandomRange(rng, 0.15f, 0.45f), RandomRange(rng, 0.75f, 1f));
            Color lowland = Color.HSVToRGB((float)rng.NextDouble(), RandomRange(rng, 0.35f, 0.85f), RandomRange(rng, 0.35f, 0.75f));
            Color highland = Color.HSVToRGB((float)rng.NextDouble(), RandomRange(rng, 0.15f, 0.7f), RandomRange(rng, 0.55f, 0.95f));
            Color peak = Color.Lerp(highland, Color.white, RandomRange(rng, 0.35f, 0.8f));
            template.biomeColorCurves = new[]
            {
                CelestialBodyTemplate.CreateGradient(shore, lowland),
                CelestialBodyTemplate.CreateGradient(lowland, highland),
                CelestialBodyTemplate.CreateGradient(highland, peak)
            };
        }

        private static void EnsureMoonGradients(MoonTemplate template, System.Random rng)
        {
            Color dark = Color.HSVToRGB((float)rng.NextDouble(), RandomRange(rng, 0f, 0.18f), RandomRange(rng, 0.12f, 0.35f));
            Color mid = Color.HSVToRGB((float)rng.NextDouble(), RandomRange(rng, 0f, 0.2f), RandomRange(rng, 0.38f, 0.65f));
            Color bright = Color.HSVToRGB((float)rng.NextDouble(), RandomRange(rng, 0f, 0.25f), RandomRange(rng, 0.72f, 0.96f));
            template.biomeColorCurves = new[]
            {
                CelestialBodyTemplate.CreateGradient(dark, mid),
                CelestialBodyTemplate.CreateGradient(mid, bright)
            };
        }

        private static NoiseLayer CreateNoise(System.Random rng, float minScale, float maxScale, float minAmplitude, float maxAmplitude, int minOctaves, int maxOctaves, bool ridgeLike = false)
        {
            return new NoiseLayer
            {
                scale = RandomRange(rng, minScale, maxScale),
                octaves = RandomRangeInt(rng, minOctaves, maxOctaves),
                amplitude = RandomRange(rng, minAmplitude, maxAmplitude),
                persistence = ridgeLike ? RandomRange(rng, 0.35f, 0.55f) : RandomRange(rng, 0.45f, 0.65f),
                lacunarity = ridgeLike ? RandomRange(rng, 1.8f, 2.4f) : RandomRange(rng, 1.8f, 2.2f),
                offset = RandomVector3(rng, 0f, 256f)
            };
        }

        private static Vector3 RandomVector3(System.Random rng, float min, float max)
        {
            return new Vector3(
                RandomRange(rng, min, max),
                RandomRange(rng, min, max),
                RandomRange(rng, min, max));
        }

        private static float RandomRange(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }

        private static int RandomRangeInt(System.Random rng, int minInclusive, int maxInclusive)
        {
            return rng.Next(minInclusive, maxInclusive + 1);
        }
    }
}
