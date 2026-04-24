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
        public const float SurfaceContactOffset = 0.0005f;
        public const float MaxIntegrationSubstep = 0.005f;
        public const float CollisionSpinDegreesPerSpeed = 12f;
        public const float MaxCollisionSpinPerStep = 1.25f;
        public const float SurfaceStickReleaseSpeed = 0.35f;
        public const float SurfaceNoBounceSpeed = 1.5f;
        public const float SurfaceDynamicFrictionPerSecond = 6.5f;
        public const float SurfaceAngularFrictionPerSecond = 12.0f;
        public const float SurfaceStaticFrictionSpeed = 0.35f;
        public const float SurfaceRestLinearSpeed = 0.12f;
        public const float SurfaceRestAngularSpeed = 8f;
        public const float MaxAngularSpeedDegPerSec = 360f;

        public const float SurfaceBounceFactor = 0.08f;
        public const float SurfaceTangentialDamping = 0.995f;
    }
}
