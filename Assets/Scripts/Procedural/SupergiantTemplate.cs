using UnityEngine;

namespace Vortex.Procedural
{
    [CreateAssetMenu(fileName = "SupergiantTemplate", menuName = "Vortex/CelestialBody/Supergiant")]
    public sealed class SupergiantTemplate : CelestialBodyTemplate
    {
        private void OnValidate()
        {
            bodyClass = BodyClass.Supergiant;
        }

        private void Reset()
        {
            bodyClass = BodyClass.Supergiant;
            generationMode = GenerationMode.StellarPlasma | GenerationMode.GasBand;
            hasSurface = false;
            supportsLanding = false;
            radiationHazard = true;
        }
    }
}
