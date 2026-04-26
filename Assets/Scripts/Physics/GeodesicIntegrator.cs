using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Jobs;

namespace Vortex.Physics
{
    public struct GeodesicBodyStateData
    {
        public float3 position;
        public quaternion rotation;
        public float3 velocity;
        public float3 angularVelocityDegPerSec;
        public float properTime;
        public float inertialMass;
        public float gravitationalMass;
        public byte contributesToGravity;
    }

    [BurstCompile]
    public struct GeodesicIntegrator : IJobParallelFor
    {
        [ReadOnly] public NativeArray<GeodesicBodyStateData> currentStates;
        [ReadOnly] public NativeArray<GravityWellData> wells;

        public NativeArray<GeodesicBodyStateData> nextStates;
        public NativeArray<float> localDeltaTimes;

        public float globalDeltaTime;
        public float fixedDeltaTime;

        public void Execute(int index)
        {
            GeodesicBodyStateData state = currentStates[index];

            float properTime = math.clamp(state.properTime, 0f, 1f);
            if (properTime <= 0f)
            {
                localDeltaTimes[index] = 0f;
                nextStates[index] = state;
                return;
            }

            float3 velocity = state.velocity;
            float speed = math.length(velocity);
            float c = PhysicsConstants.SpeedOfLight;
            float speedRatio = c > 0f ? speed / c : 0f;

            float softRatio = PhysicsConstants.SoftSpeedLimitRatio;
            float hardRatio = PhysicsConstants.HardSpeedLimitRatio;
            float softFactor = 1f;
            if (speedRatio > softRatio)
            {
                float t = math.saturate((speedRatio - softRatio) / math.max(PhysicsConstants.IntegrationEpsilon, hardRatio - softRatio));
                softFactor = math.lerp(1f, 0.1f, t);
            }

            if (speedRatio > hardRatio && speed > PhysicsConstants.IntegrationEpsilon)
            {
                velocity = math.normalize(velocity) * (hardRatio * c);
            }

            float srFactor = math.sqrt(math.max(PhysicsConstants.MinRelativityFactor, 1f - speedRatio * speedRatio));
            float grFactor = ComputeGrFactor(index, state.position);
            float localDeltaTime = globalDeltaTime * properTime * srFactor * grFactor * softFactor;
            localDeltaTimes[index] = math.max(0f, localDeltaTime);

            if (localDeltaTime <= 0f)
            {
                state.velocity = velocity;
                nextStates[index] = state;
                return;
            }

            float maxSubstep = math.max(PhysicsConstants.IntegrationEpsilon, PhysicsConstants.MaxIntegrationSubstep);
            int substepCount = math.clamp((int)math.ceil(localDeltaTime / maxSubstep), 1, 128);
            float substepDt = localDeltaTime / substepCount;

            float3 position = state.position;
            quaternion rotation = state.rotation;
            float3 angularVelocity = state.angularVelocityDegPerSec;

            for (int step = 0; step < substepCount; step++)
            {
                IntegrateRk4(index, ref position, ref velocity, ref rotation, ref angularVelocity, substepDt);
            }

            float hardSpeed = PhysicsConstants.HardSpeedLimitRatio * PhysicsConstants.SpeedOfLight;
            float nextSpeed = math.length(velocity);
            if (nextSpeed > hardSpeed && nextSpeed > PhysicsConstants.IntegrationEpsilon)
            {
                velocity = math.normalize(velocity) * hardSpeed;
            }

            state.position = position;
            state.rotation = math.normalize(rotation);
            state.velocity = velocity;
            state.angularVelocityDegPerSec = angularVelocity;
            nextStates[index] = state;
        }

        private void IntegrateRk4(int bodyIndex, ref float3 position, ref float3 velocity, ref quaternion rotation, ref float3 angularVelocity, float dt)
        {
            float3 p0 = position;
            float3 v0 = velocity;

            float3 a1 = ComputeAcceleration(bodyIndex, p0, v0);
            float3 k1v = a1 * dt;
            float3 k1p = v0 * dt;

            float3 a2 = ComputeAcceleration(bodyIndex, p0 + 0.5f * k1p, v0 + 0.5f * k1v);
            float3 k2v = a2 * dt;
            float3 k2p = (v0 + 0.5f * k1v) * dt;

            float3 a3 = ComputeAcceleration(bodyIndex, p0 + 0.5f * k2p, v0 + 0.5f * k2v);
            float3 k3v = a3 * dt;
            float3 k3p = (v0 + 0.5f * k2v) * dt;

            float3 a4 = ComputeAcceleration(bodyIndex, p0 + k3p, v0 + k3v);
            float3 k4v = a4 * dt;
            float3 k4p = (v0 + k3v) * dt;

            float3 nextVelocity = v0 + (k1v + 2f * k2v + 2f * k3v + k4v) / 6f;
            float3 nextPosition = p0 + (k1p + 2f * k2p + 2f * k3p + k4p) / 6f;

            if (math.lengthsq(angularVelocity) > PhysicsConstants.IntegrationEpsilon)
            {
                float stepDegrees = math.length(angularVelocity) * dt;
                float3 axis = math.normalize(angularVelocity);
                quaternion dq = quaternion.AxisAngle(axis, math.radians(stepDegrees));
                rotation = math.normalize(math.mul(dq, rotation));
            }

            float fixedDtSafe = math.max(PhysicsConstants.IntegrationEpsilon, fixedDeltaTime);
            float dampingPerSecond = math.pow(math.saturate(PhysicsConstants.SurfaceTangentialDamping), 1f / fixedDtSafe);
            angularVelocity *= math.pow(dampingPerSecond, dt);

            float maxAngularSpeed = PhysicsConstants.MaxAngularSpeedDegPerSec;
            float angularSpeed = math.length(angularVelocity);
            if (angularSpeed > maxAngularSpeed && angularSpeed > PhysicsConstants.IntegrationEpsilon)
            {
                angularVelocity = math.normalize(angularVelocity) * maxAngularSpeed;
            }

            position = nextPosition;
            velocity = nextVelocity;
        }

        private float ComputeGrFactor(int bodyIndex, float3 position)
        {
            float minFactor = 1f;

            for (int i = 0; i < wells.Length; i++)
            {
                GravityWellData well = wells[i];
                float3 delta = position - well.position;
                float r = math.length(delta);
                float minRadius = math.max(well.schwarzschildRadius + PhysicsConstants.IntegrationEpsilon, PhysicsConstants.IntegrationEpsilon);
                r = math.max(r, minRadius);

                float term = 1f - (well.schwarzschildRadius / r);
                float factor = math.sqrt(math.max(PhysicsConstants.MinRelativityFactor, term));
                minFactor = math.min(minFactor, factor);
            }

            for (int i = 0; i < currentStates.Length; i++)
            {
                if (i == bodyIndex)
                {
                    continue;
                }

                GeodesicBodyStateData sourceBody = currentStates[i];
                if (sourceBody.contributesToGravity == 0 || sourceBody.gravitationalMass <= 0f)
                {
                    continue;
                }

                float c2 = PhysicsConstants.SpeedOfLight * PhysicsConstants.SpeedOfLight;
                float sourceRs = 2f * PhysicsConstants.GravitationalConstant * sourceBody.gravitationalMass / c2;
                float3 delta = position - sourceBody.position;
                float r = math.length(delta);
                float minRadius = math.max(sourceRs + PhysicsConstants.IntegrationEpsilon, PhysicsConstants.IntegrationEpsilon);
                r = math.max(r, minRadius);

                float term = 1f - (sourceRs / r);
                float factor = math.sqrt(math.max(PhysicsConstants.MinRelativityFactor, term));
                minFactor = math.min(minFactor, factor);
            }

            return minFactor;
        }

        private float3 ComputeAcceleration(int bodyIndex, float3 position, float3 velocity)
        {
            float3 acceleration = float3.zero;

            for (int i = 0; i < wells.Length; i++)
            {
                GravityWellData well = wells[i];
                if (well.mass <= 0f)
                {
                    continue;
                }

                float3 toWell = well.position - position;
                float minRadius = math.max(
                    math.max(well.physicalRadius, well.schwarzschildRadius + PhysicsConstants.IntegrationEpsilon),
                    PhysicsConstants.IntegrationEpsilon);

                float sqrDistance = math.max(math.lengthsq(toWell), minRadius * minRadius);
                float distance = math.sqrt(sqrDistance);
                float3 radialDir = toWell / distance;

                float newtonA = PhysicsConstants.GravitationalConstant * well.mass / sqrDistance;
                float christoffelA = ComputeSchwarzschildChristoffelCorrection(well.schwarzschildRadius, distance, radialDir, velocity);

                acceleration += radialDir * (newtonA + christoffelA);
            }

            for (int i = 0; i < currentStates.Length; i++)
            {
                if (i == bodyIndex)
                {
                    continue;
                }

                GeodesicBodyStateData sourceBody = currentStates[i];
                if (sourceBody.contributesToGravity == 0 || sourceBody.gravitationalMass <= 0f)
                {
                    continue;
                }

                float3 toSource = sourceBody.position - position;
                float sourceRadius = math.max(0.5f, PhysicsConstants.IntegrationEpsilon);
                float sqrDistance = math.max(math.lengthsq(toSource), sourceRadius * sourceRadius);
                float distance = math.sqrt(sqrDistance);
                float3 direction = toSource / distance;

                float a = PhysicsConstants.GravitationalConstant * sourceBody.gravitationalMass / sqrDistance;
                acceleration += direction * a;
            }

            return acceleration;
        }

        private static float ComputeSchwarzschildChristoffelCorrection(float rs, float r, float3 radialDir, float3 velocity)
        {
            if (rs <= PhysicsConstants.IntegrationEpsilon || r <= rs + PhysicsConstants.IntegrationEpsilon)
            {
                return 0f;
            }

            float c = PhysicsConstants.SpeedOfLight;
            float c2 = c * c;
            float radialSpeed = math.dot(velocity, radialDir);
            float3 tangential = velocity - radialDir * radialSpeed;
            float tangentialSpeedSq = math.lengthsq(tangential);

            float oneMinusRsOverR = math.max(PhysicsConstants.MinRelativityFactor, 1f - (rs / r));
            float gammaRtt = (rs * c2 * oneMinusRsOverR) / (2f * r * r);
            float gammaRrr = -rs / (2f * r * (r - rs));
            float gammaRphiphi = -(r - rs);
            float omegaSq = tangentialSpeedSq / math.max(PhysicsConstants.IntegrationEpsilon, r * r);

            float correction = (-gammaRtt / c2) - (gammaRrr * radialSpeed * radialSpeed) - (gammaRphiphi * omegaSq / math.max(PhysicsConstants.IntegrationEpsilon, r));
            return math.max(-math.abs(gammaRtt), correction);
        }
    }

    [BurstCompile]
    public struct GeodesicTransformApplyJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<GeodesicBodyStateData> states;

        public void Execute(int index, TransformAccess transform)
        {
            GeodesicBodyStateData state = states[index];
            transform.position = state.position;
            transform.rotation = state.rotation;
        }
    }
}
