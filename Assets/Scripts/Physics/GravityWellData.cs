using Unity.Mathematics;
using UnityEngine;

namespace Vortex.Physics
{
    public struct GravityWellData
    {
        public float3 position;
        public float mass;
        public float schwarzschildRadius;
        public float physicalRadius;
    }
}
