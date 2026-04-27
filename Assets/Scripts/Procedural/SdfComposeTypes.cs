using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Vortex.Procedural
{
    public enum SdfComposeOperation
    {
        Union = 0,
        Blend = 1,
        Subtract = 2,
        Mask = 3
    }

    public enum SdfComposeShape
    {
        Sphere = 0,
        Capsule = 1
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SdfComposeCommand
    {
        public int operation;
        public int shape;
        public Vector3 pointA;
        public float radiusA;
        public Vector3 pointB;
        public float radiusB;
        public float strength;
        public float falloff;
        public int flags;
        public int otherBodyId;

        public static int Stride => Marshal.SizeOf<SdfComposeCommand>();

        public static SdfComposeCommand CreateBlendSphere(Vector3 center, float radius, float strength)
        {
            return new SdfComposeCommand
            {
                operation = (int)SdfComposeOperation.Blend,
                shape = (int)SdfComposeShape.Sphere,
                pointA = center,
                radiusA = radius,
                strength = strength,
                falloff = 1f
            };
        }

        public static SdfComposeCommand CreateSubtractSphere(Vector3 center, float radius, float strength)
        {
            return new SdfComposeCommand
            {
                operation = (int)SdfComposeOperation.Subtract,
                shape = (int)SdfComposeShape.Sphere,
                pointA = center,
                radiusA = radius,
                strength = strength,
                falloff = 1f
            };
        }
    }

    [Serializable]
    public struct DamageStamp
    {
        public SdfComposeShape shape;
        public Vector3 localPointA;
        public Vector3 localPointB;
        [Min(0.01f)] public float radius;
        [Min(0f)] public float depth;

        public SdfComposeCommand ToComposeCommand()
        {
            return new SdfComposeCommand
            {
                operation = (int)SdfComposeOperation.Subtract,
                shape = (int)shape,
                pointA = localPointA,
                pointB = localPointB,
                radiusA = radius,
                radiusB = radius,
                strength = Mathf.Max(0.001f, depth),
                falloff = 1f
            };
        }
    }
}
