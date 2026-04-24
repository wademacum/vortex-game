using UnityEngine;

namespace Vortex.Procedural
{
    [CreateAssetMenu(fileName = "AsteroidClusterTemplate", menuName = "Vortex/CelestialBody/AsteroidCluster")]
    public sealed class AsteroidClusterTemplate : CelestialBodyTemplate
    {
        private void OnValidate()
        {
            bodyClass = BodyClass.AsteroidCluster;
        }

        private void Reset()
        {
            bodyClass = BodyClass.AsteroidCluster;
            generationMode = GenerationMode.SolidSdf;
            hasSurface = true;
            supportsLanding = false;
        }
    }
}
