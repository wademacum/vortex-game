using System;
using UnityEngine;

namespace Vortex.Procedural
{
    public enum ShapeModel
    {
        Generic = 0,
        Planet = 1,
        Moon = 2
    }

    public enum ShadingModel
    {
        VertexColor = 0,
        PlanetBands = 1,
        MoonBiomes = 2
    }

    [Serializable]
    public struct BaseShapeConfig
    {
        public Vector3 commonOffset;
        [Range(-0.5f, 0.5f)] public float radiusBias;
        [Min(0.1f)] public float verticalSquash;
    }

    [Serializable]
    public struct PlanetShapeConfig
    {
        public NoiseLayer continent;
        public NoiseLayer mountain;
        public NoiseLayer detail;
        public NoiseLayer mask;
        [Min(0f)] public float oceanDepthMultiplier;
        [Min(0f)] public float oceanFloorDepth;
        [Min(0f)] public float oceanFloorSmoothing;
        [Min(0f)] public float mountainBlend;
    }

    [Serializable]
    public struct MoonShapeConfig
    {
        public NoiseLayer shape;
        public NoiseLayer ridgeA;
        public NoiseLayer ridgeB;
        public int craterCount;
        public Vector2 craterRadiusRange;
        public float craterDepth;
        public float craterRimSharpness;
        public float craterNoiseScale;
    }

    [Serializable]
    public struct AsteroidShapeConfig
    {
        public NoiseLayer baseShape;
        public NoiseLayer detailA;
        public NoiseLayer detailB;
        public int pitCount;
        public Vector2 pitRadiusRange;
        public float pitDepth;
        public float pitRimSharpness;
        public float surfaceIrregularity;
    }

    [Serializable]
    public struct AsteroidShadingConfig
    {
        public int albedoSpotCount;
        public Vector2 albedoSpotRadiusRange;
        public NoiseLayer albedoSpotNoise;
        public NoiseLayer surfaceDetailNoise;
        public float metallicVariation;
        public float smoothnessVariation;
    }

    [Serializable]
    public struct PlanetShadingConfig
    {
        public NoiseLayer largeNoise;
        public NoiseLayer smallNoise;
        public NoiseLayer detailNoise;
        public NoiseLayer detailWarpNoise;
    }

    [Serializable]
    public struct MoonShadingConfig
    {
        public int biomePointCount;
        public Vector2 biomeRadiusRange;
        public NoiseLayer biomeWarpNoise;
        public NoiseLayer detailNoise;
        public NoiseLayer detailWarpNoise;
        public float candidatePoolSize;
        public int desiredEjectaRays;
        public float ejectaRaysScale;
    }

    [Serializable]
    public struct MoonSurfaceConfig
    {
        public Texture2D mainTexture;
        public Texture2D craterRayTexture;
        public Texture2D flatSurfaceTexture;
        public Texture2D steepSurfaceTexture;
        public float mainTextureScale;
        public float flatSurfaceScale;
        public float steepSurfaceScale;
        public float textureBlendStrength;
        public float ejectaBrightness;
        public float steepDarkening;
    }
}
