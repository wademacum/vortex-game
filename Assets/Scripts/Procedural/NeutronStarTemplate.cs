using UnityEngine;

namespace Vortex.Procedural
{
    [CreateAssetMenu(fileName = "NeutronStarTemplate", menuName = "Vortex/CelestialBody/NeutronStar")]
    public sealed class NeutronStarTemplate : CelestialBodyTemplate
    {
        private void OnValidate()
        {
            bodyClass = BodyClass.NeutronStar;
        }

        private void Reset()
        {
            bodyClass = BodyClass.NeutronStar;
            generationMode = GenerationMode.CompactObject | GenerationMode.StellarPlasma;
            hasSurface = false;
            supportsLanding = false;
            radiationHazard = true;
        }
    }
}
