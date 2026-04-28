using System;
using UnityEngine;

namespace Vortex.Procedural
{
    public enum ShapeModel
    {
        Generic = 0,
        Planet = 1,
        Moon = 2,
        Asteroid = 3
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
        [Min(0f)] public float continentFloor;
        [Min(0f)] public float continentStrength;
        [Min(0f)] public float mountainStrength;
        [Min(0f)] public float detailStrength;
        [Min(0f)] public float oceanDepthMultiplier;
        [Min(0f)] public float oceanFloorDepth;
        [Min(0f)] public float oceanFloorSmoothing;
        [Min(0f)] public float mountainBlend;
    }

    [Serializable]
    public struct MoonShapeConfig
    {
        public int craterCount;
        public Vector2 craterRadiusRange;
        [Range(0f, 1f)] public float craterRadiusBias;
        public float craterDepth;
        public Vector2 craterFloorHeightRange;
        [Range(0f, 1f)] public float craterFloorRadius;
        public float craterWallSmoothness;
        public float craterRimWidth;
        public float craterRimHeight;
        public float craterRimSharpness;
        public float craterEdgeWarpFrequency;
        public float craterEdgeWarpStrength;
        [Range(0f, 1f)] public float craterCrowdingRadiusScale;
        [Range(0f, 1f)] public float craterDistributionJitter;
        [Range(0f, 1f)] public float youngCraterFraction;
    }

    [Serializable]
    public struct MoonTerrainNoiseConfig
    {
        public NoiseLayer macroShape;
        public NoiseLayer ridgeNoise;
        public NoiseLayer detailNoise;
        public NoiseLayer warpNoise;
        public float warpStrength;
        public float macroStrength;
        public float ridgeStrength;
        public float detailStrength;
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
    public struct MoonBiomeConfig
    {
        public NoiseLayer biomeWarpNoise;
        public float mariaBias;
        public float highlandBias;
        public float colorVariation;
    }

    [Serializable]
    public struct MoonSurfaceConfig
    {
        public Texture2D mainTexture;
        public Texture2D craterRayTexture;
        public Texture2D flatSurfaceTexture;
        public Texture2D steepSurfaceTexture;
        public Texture2D flatNormalMap;
        public Texture2D steepNormalMap;
        public float mainTextureScale;
        public float flatSurfaceScale;
        public float steepSurfaceScale;
        public float microDetailScale;
        public float textureBlendStrength;
        public float normalBlendStrength;
        public float microDetailStrength;
        public float flatContrast;
        public float steepContrast;
        public float albedoSaturation;
        public float ejectaBrightness;
        public float steepDarkening;
    }
}
