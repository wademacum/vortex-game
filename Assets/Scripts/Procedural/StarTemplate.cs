using UnityEngine;

namespace Vortex.Procedural
{
    [CreateAssetMenu(fileName = "StarTemplate", menuName = "Vortex/CelestialBody/Star")]
    public sealed class StarTemplate : CelestialBodyTemplate
    {
        private void OnValidate()
        {
            bodyClass = BodyClass.Star;
            NotifyTemplateChanged();
        }

        private void Reset()
        {
            bodyClass = BodyClass.Star;
            generationMode = GenerationMode.StellarPlasma;
            hasSurface = false;
            supportsLanding = false;
            radiationHazard = true;
            NotifyTemplateChanged();
        }
    }
}
