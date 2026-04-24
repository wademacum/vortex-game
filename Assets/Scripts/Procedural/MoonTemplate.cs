using UnityEngine;

namespace Vortex.Procedural
{
    [CreateAssetMenu(fileName = "MoonTemplate", menuName = "Vortex/CelestialBody/Moon")]
    public sealed class MoonTemplate : CelestialBodyTemplate
    {
        private void OnValidate()
        {
            bodyClass = BodyClass.Moon;
        }

        private void Reset()
        {
            bodyClass = BodyClass.Moon;
            generationMode = GenerationMode.SolidSdf;
            hasSurface = true;
            supportsLanding = true;
        }
    }
}
