namespace Vortex.Physics
{
    public static class PhysicsConstants
    {
        public const float GravitationalConstant = 1f;
        public const float SpeedOfLight = 100f;

        public const float GameplaySpeedMinRatio = 0.60f;
        public const float GameplaySpeedMaxRatio = 0.80f;
        public const float SoftSpeedLimitRatio = 0.85f;
        public const float HardSpeedLimitRatio = 0.95f;

        public const float MinRelativityFactor = 0.01f;
        public const float IntegrationEpsilon = 0.0001f;

        public const float SurfaceBounceFactor = 0.15f;
        public const float SurfaceTangentialDamping = 0.98f;
    }
}
