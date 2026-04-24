using System;
using UnityEngine;

namespace Vortex.Procedural
{
    [Serializable]
    public struct NoiseLayer
    {
        [Min(0f)] public float scale;
        public int octaves;
        public float amplitude;
        public float persistence;
        public float lacunarity;
        public Vector3 offset;
    }

    [Serializable]
    public struct NoiseLayerConfig
    {
        public NoiseLayer continent;
        public NoiseLayer mountain;
        public NoiseLayer detail;
    }
}
