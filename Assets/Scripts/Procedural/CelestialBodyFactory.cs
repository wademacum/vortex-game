using System;
using UnityEngine;

namespace Vortex.Procedural
{
    public static class CelestialBodyFactory
    {
        public static RuntimeBodyData Generate(int seed, BodyClass type, CelestialBodyTemplate[] pool)
        {
            if (pool == null || pool.Length == 0)
            {
                throw new ArgumentException("Template pool is null or empty.", nameof(pool));
            }

            int rngSeed = ToNonZeroSeed(seed);
            System.Random rng = new System.Random(rngSeed);

            CelestialBodyTemplate selected = SelectTemplate(type, pool, ref rng);
            return BuildRuntimeData(selected, ref rng);
        }

        private static CelestialBodyTemplate SelectTemplate(BodyClass type, CelestialBodyTemplate[] pool, ref System.Random rng)
        {
            float totalWeight = 0f;
            for (int i = 0; i < pool.Length; i++)
            {
                CelestialBodyTemplate template = pool[i];
                if (template == null || template.bodyClass != type)
                {
                    continue;
                }

                totalWeight += Mathf.Max(0f, template.spawnWeight);
            }

            if (totalWeight <= 0f)
            {
                throw new InvalidOperationException($"No valid templates found for body type {type}.");
            }

            float pick = NextFloat(rng, 0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < pool.Length; i++)
            {
                CelestialBodyTemplate template = pool[i];
                if (template == null || template.bodyClass != type)
                {
                    continue;
                }

                cumulative += Mathf.Max(0f, template.spawnWeight);
                if (pick <= cumulative)
                {
                    return template;
                }
            }

            for (int i = pool.Length - 1; i >= 0; i--)
            {
                CelestialBodyTemplate template = pool[i];
                if (template != null && template.bodyClass == type)
                {
                    return template;
                }
            }

            throw new InvalidOperationException($"Unable to select template for body type {type}.");
        }

        private static RuntimeBodyData BuildRuntimeData(CelestialBodyTemplate template, ref System.Random rng)
        {
            return new RuntimeBodyData
            {
                bodyClass = template.bodyClass,
                generationMode = template.generationMode,
                mass = SampleRange(template.massRange, ref rng),
                radius = SampleRange(template.radiusRange, ref rng),
                density = SampleRange(template.densityRange, ref rng),
                rotationSpeed = SampleRange(template.rotationRange, ref rng),
                temperature = SampleRange(template.temperatureRange, ref rng),
                albedo = SampleRange(template.albedoRange, ref rng),
                anomalyChance = Mathf.Clamp01(template.anomalyChance),
                hasSurface = template.hasSurface,
                hasAtmosphere = template.hasAtmosphere,
                hasEventHorizon = template.hasEventHorizon,
                supportsLanding = template.supportsLanding,
                radiationHazard = template.radiationHazard,
                noiseLayerConfig = template.noiseLayerConfig,
                biomeColorCurves = template.biomeColorCurves,
                emissiveRange = template.emissiveRange
            };
        }

        private static float SampleRange(Vector2 range, ref System.Random rng)
        {
            float min = Mathf.Min(range.x, range.y);
            float max = Mathf.Max(range.x, range.y);
            return NextFloat(rng, min, max);
        }

        private static float NextFloat(System.Random rng, float min, float max)
        {
            if (max <= min)
            {
                return min;
            }

            double t = rng.NextDouble();
            return min + (float)t * (max - min);
        }

        private static int ToNonZeroSeed(int seed)
        {
            if (seed == 0)
            {
                return 1;
            }

            return seed;
        }
    }
}
