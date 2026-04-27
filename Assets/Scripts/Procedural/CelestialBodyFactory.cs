using System;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

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
            Random rng = new Random((uint)rngSeed);

            CelestialBodyTemplate selected = SelectTemplate(type, pool, ref rng);
            return BuildRuntimeData(selected, ref rng, randomizeRanges: true);
        }

        public static RuntimeBodyData GenerateFromTemplate(int seed, CelestialBodyTemplate template, bool randomizeRanges)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            int rngSeed = ToNonZeroSeed(seed);
            Random rng = new Random((uint)rngSeed);
            return BuildRuntimeData(template, ref rng, randomizeRanges);
        }

        private static CelestialBodyTemplate SelectTemplate(BodyClass type, CelestialBodyTemplate[] pool, ref Random rng)
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

            float pick = NextFloat(ref rng, 0f, totalWeight);
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

        private static RuntimeBodyData BuildRuntimeData(CelestialBodyTemplate template, ref Random rng, bool randomizeRanges)
        {
            return new RuntimeBodyData
            {
                bodyClass = template.bodyClass,
                generationMode = template.generationMode,
            mass = randomizeRanges ? SampleRange(template.massRange, ref rng) : Midpoint(template.massRange),
            radius = randomizeRanges ? SampleRange(template.radiusRange, ref rng) : Midpoint(template.radiusRange),
            density = randomizeRanges ? SampleRange(template.densityRange, ref rng) : Midpoint(template.densityRange),
            rotationSpeed = randomizeRanges ? SampleRange(template.rotationRange, ref rng) : Midpoint(template.rotationRange),
            temperature = randomizeRanges ? SampleRange(template.temperatureRange, ref rng) : Midpoint(template.temperatureRange),
            albedo = randomizeRanges ? SampleRange(template.albedoRange, ref rng) : Midpoint(template.albedoRange),
                anomalyChance = Mathf.Clamp01(template.anomalyChance),
                hasSurface = template.hasSurface,
                hasAtmosphere = template.hasAtmosphere,
                hasEventHorizon = template.hasEventHorizon,
                supportsLanding = template.supportsLanding,
                radiationHazard = template.radiationHazard,
                corePressureSupport = Mathf.Max(0f, template.corePressureSupport),
                fractureThreshold = Mathf.Max(0f, template.fractureThreshold),
                collapseThreshold = Mathf.Max(0f, template.collapseThreshold),
                novaThreshold = Mathf.Max(0f, template.novaThreshold),
                structuralDamping = Mathf.Max(0f, template.structuralDamping),
                enableMeshNodeDeformation = template.enableMeshNodeDeformation,
                meshTidalStartThreshold = Mathf.Max(0f, template.meshTidalStartThreshold),
                meshTidalMaxThreshold = Mathf.Max(0f, template.meshTidalMaxThreshold),
                meshAxialStretchAtFull = Mathf.Max(0f, template.meshAxialStretchAtFull),
                meshRadialSqueezeAtFull = Mathf.Max(0f, template.meshRadialSqueezeAtFull),
                noiseLayerConfig = template.noiseLayerConfig,
                biomeColorCurves = template.biomeColorCurves,
                emissiveRange = template.emissiveRange
            };
        }

        private static float SampleRange(Vector2 range, ref Random rng)
        {
            float min = Mathf.Min(range.x, range.y);
            float max = Mathf.Max(range.x, range.y);
            return NextFloat(ref rng, min, max);
        }

        private static float Midpoint(Vector2 range)
        {
            float min = Mathf.Min(range.x, range.y);
            float max = Mathf.Max(range.x, range.y);
            return (min + max) * 0.5f;
        }

        private static float NextFloat(ref Random rng, float min, float max)
        {
            if (max <= min)
            {
                return min;
            }

            return rng.NextFloat(min, max);
        }

        private static int ToNonZeroSeed(int seed)
        {
            unchecked
            {
                // Unity.Mathematics.Random requires a non-zero uint seed.
                uint raw = (uint)seed;
                uint mixed = raw * 747796405u + 2891336453u;
                if (mixed == 0u)
                {
                    mixed = 1u;
                }

                return (int)mixed;
            }
        }
    }
}
