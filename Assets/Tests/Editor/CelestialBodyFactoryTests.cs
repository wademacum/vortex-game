using NUnit.Framework;
using UnityEngine;
using Vortex.Procedural;

namespace Vortex.Tests.Editor
{
    public sealed class CelestialBodyFactoryTests
    {
        [Test]
        public void Generate_WithSameSeed_ReturnsSameRuntimeData()
        {
            PlanetTemplate template = CreatePlanetTemplate(1f);
            CelestialBodyTemplate[] pool = { template };

            RuntimeBodyData first = CelestialBodyFactory.Generate(12345, BodyClass.Planet, pool);
            RuntimeBodyData second = CelestialBodyFactory.Generate(12345, BodyClass.Planet, pool);

            Assert.AreEqual(first.bodyClass, second.bodyClass);
            Assert.AreEqual(first.mass, second.mass);
            Assert.AreEqual(first.radius, second.radius);
            Assert.AreEqual(first.density, second.density);
            Assert.AreEqual(first.rotationSpeed, second.rotationSpeed);
            Assert.AreEqual(first.temperature, second.temperature);
            Assert.AreEqual(first.albedo, second.albedo);
        }

        [Test]
        public void Generate_WithDifferentSeeds_ReturnsDifferentSample()
        {
            PlanetTemplate template = CreatePlanetTemplate(1f);
            CelestialBodyTemplate[] pool = { template };

            RuntimeBodyData first = CelestialBodyFactory.Generate(42, BodyClass.Planet, pool);
            RuntimeBodyData second = CelestialBodyFactory.Generate(43, BodyClass.Planet, pool);

            bool anyDifference =
                !Mathf.Approximately(first.mass, second.mass) ||
                !Mathf.Approximately(first.radius, second.radius) ||
                !Mathf.Approximately(first.rotationSpeed, second.rotationSpeed);

            Assert.IsTrue(anyDifference, "Different seeds should produce different runtime samples.");
        }

        [Test]
        public void Generate_WithNoMatchingBodyType_Throws()
        {
            BlackHoleTemplate template = ScriptableObject.CreateInstance<BlackHoleTemplate>();
            template.bodyClass = BodyClass.BlackHole;
            template.spawnWeight = 1f;

            CelestialBodyTemplate[] pool = { template };

            Assert.Throws<System.InvalidOperationException>(
                () => CelestialBodyFactory.Generate(10, BodyClass.Planet, pool));
        }

        private static PlanetTemplate CreatePlanetTemplate(float weight)
        {
            PlanetTemplate template = ScriptableObject.CreateInstance<PlanetTemplate>();
            template.bodyClass = BodyClass.Planet;
            template.spawnWeight = weight;
            template.massRange = new Vector2(100f, 200f);
            template.radiusRange = new Vector2(10f, 30f);
            template.densityRange = new Vector2(1f, 4f);
            template.rotationRange = new Vector2(0.5f, 3f);
            template.temperatureRange = new Vector2(220f, 380f);
            template.albedoRange = new Vector2(0.2f, 0.8f);
            template.anomalyChance = 0.15f;
            return template;
        }
    }
}
