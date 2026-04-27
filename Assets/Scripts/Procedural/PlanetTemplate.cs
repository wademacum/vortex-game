using UnityEngine;

namespace Vortex.Procedural
{
    [CreateAssetMenu(fileName = "PlanetTemplate", menuName = "Vortex/CelestialBody/Planet")]
    public sealed class PlanetTemplate : CelestialBodyTemplate
    {
        private void OnValidate()
        {
            bodyClass = BodyClass.Planet;
            generationMode |= GenerationMode.SolidSdf;
            EnsureSolidSdfNoiseDefaults();
            NotifyTemplateChanged();
        }

        private void Reset()
        {
            bodyClass = BodyClass.Planet;
            generationMode = GenerationMode.SolidSdf;
            hasSurface = true;
            supportsLanding = true;
            EnsureSolidSdfNoiseDefaults();
            NotifyTemplateChanged();
        }
    }
}
