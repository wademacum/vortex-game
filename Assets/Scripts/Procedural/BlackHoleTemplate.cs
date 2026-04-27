using UnityEngine;

namespace Vortex.Procedural
{
    [CreateAssetMenu(fileName = "BlackHoleTemplate", menuName = "Vortex/CelestialBody/BlackHole")]
    public sealed class BlackHoleTemplate : CelestialBodyTemplate
    {
        private void OnValidate()
        {
            bodyClass = BodyClass.BlackHole;
            NotifyTemplateChanged();
        }

        private void Reset()
        {
            bodyClass = BodyClass.BlackHole;
            generationMode = GenerationMode.CompactObject | GenerationMode.AccretionDisk;
            hasSurface = false;
            hasEventHorizon = true;
            supportsLanding = false;
            radiationHazard = true;
            NotifyTemplateChanged();
        }
    }
}
