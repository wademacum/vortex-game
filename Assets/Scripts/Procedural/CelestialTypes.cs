using System;

namespace Vortex.Procedural
{
    public enum BodyClass
    {
        Planet,
        Moon,
        Star,
        NeutronStar,
        BlackHole,
        Supergiant,
        AsteroidCluster
    }

    [Flags]
    public enum GenerationMode
    {
        None = 0,
        SolidSdf = 1 << 0,
        GasBand = 1 << 1,
        StellarPlasma = 1 << 2,
        CompactObject = 1 << 3,
        AccretionDisk = 1 << 4
    }
}
