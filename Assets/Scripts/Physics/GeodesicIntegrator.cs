using UnityEngine;

namespace Vortex.Physics
{
    public static class GeodesicIntegrator
    {
        public static void Integrate(RelativisticBody body, System.Collections.Generic.IReadOnlyList<GravityWell> wells, float globalDeltaTime)
        {
            if (body == null)
            {
                return;
            }

            float properTime = body.ProperTime;
            if (properTime <= 0f)
            {
                body.SetLocalDeltaTime(0f);
                return;
            }

            Vector4 fourVelocity = body.FourVelocity;
            Vector3 velocity = new Vector3(fourVelocity.x, fourVelocity.y, fourVelocity.z);

            float speed = velocity.magnitude;
            float c = PhysicsConstants.SpeedOfLight;
            float speedRatio = c > 0f ? speed / c : 0f;

            float softRatio = PhysicsConstants.SoftSpeedLimitRatio;
            float hardRatio = PhysicsConstants.HardSpeedLimitRatio;
            float softFactor = 1f;
            if (speedRatio > softRatio)
            {
                float t = Mathf.InverseLerp(softRatio, hardRatio, speedRatio);
                softFactor = Mathf.Lerp(1f, 0.1f, t);
            }

            if (speedRatio > hardRatio && speed > PhysicsConstants.IntegrationEpsilon)
            {
                velocity = velocity.normalized * (hardRatio * c);
            }

            float srFactor = Mathf.Sqrt(Mathf.Max(PhysicsConstants.MinRelativityFactor, 1f - speedRatio * speedRatio));
            float grFactor = ComputeGrFactor(body.transform.position, wells);
            float localDeltaTime = globalDeltaTime * properTime * srFactor * grFactor * softFactor;
            body.SetLocalDeltaTime(localDeltaTime);

            if (localDeltaTime <= 0f)
            {
                return;
            }

            IntegrateRk4(body, velocity, wells, localDeltaTime);
        }

        private static void IntegrateRk4(RelativisticBody body, Vector3 velocity, System.Collections.Generic.IReadOnlyList<GravityWell> wells, float dt)
        {
            Vector3 p0 = body.transform.position;
            Vector3 v0 = velocity;

            Vector3 a1 = ComputeAcceleration(p0, wells);
            Vector3 k1v = a1 * dt;
            Vector3 k1p = v0 * dt;

            Vector3 a2 = ComputeAcceleration(p0 + 0.5f * k1p, wells);
            Vector3 k2v = a2 * dt;
            Vector3 k2p = (v0 + 0.5f * k1v) * dt;

            Vector3 a3 = ComputeAcceleration(p0 + 0.5f * k2p, wells);
            Vector3 k3v = a3 * dt;
            Vector3 k3p = (v0 + 0.5f * k2v) * dt;

            Vector3 a4 = ComputeAcceleration(p0 + k3p, wells);
            Vector3 k4v = a4 * dt;
            Vector3 k4p = (v0 + k3v) * dt;

            Vector3 nextVelocity = v0 + (k1v + 2f * k2v + 2f * k3v + k4v) / 6f;
            Vector3 nextPosition = p0 + (k1p + 2f * k2p + 2f * k3p + k4p) / 6f;

            ResolveSurfaceContacts(ref nextPosition, ref nextVelocity, wells);

            float hardSpeed = PhysicsConstants.HardSpeedLimitRatio * PhysicsConstants.SpeedOfLight;
            if (nextVelocity.magnitude > hardSpeed && nextVelocity.sqrMagnitude > PhysicsConstants.IntegrationEpsilon)
            {
                nextVelocity = nextVelocity.normalized * hardSpeed;
            }

            body.transform.position = nextPosition;
            body.SphericalPosition = CartesianToSpherical(nextPosition);

            Vector4 fourVelocity = body.FourVelocity;
            fourVelocity.x = nextVelocity.x;
            fourVelocity.y = nextVelocity.y;
            fourVelocity.z = nextVelocity.z;
            body.FourVelocity = fourVelocity;
        }

        private static float ComputeGrFactor(Vector3 position, System.Collections.Generic.IReadOnlyList<GravityWell> wells)
        {
            if (wells == null || wells.Count == 0)
            {
                return 1f;
            }

            float minFactor = 1f;
            for (int i = 0; i < wells.Count; i++)
            {
                GravityWell well = wells[i];
                if (well == null)
                {
                    continue;
                }

                float r = Vector3.Distance(position, well.transform.position);
                float minRadius = Mathf.Max(well.SchwarzschildRadius + PhysicsConstants.IntegrationEpsilon, PhysicsConstants.IntegrationEpsilon);
                r = Mathf.Max(r, minRadius);
                float term = 1f - (well.SchwarzschildRadius / r);
                float factor = Mathf.Sqrt(Mathf.Max(PhysicsConstants.MinRelativityFactor, term));
                if (factor < minFactor)
                {
                    minFactor = factor;
                }
            }

            return minFactor;
        }

        private static Vector3 ComputeAcceleration(Vector3 position, System.Collections.Generic.IReadOnlyList<GravityWell> wells)
        {
            Vector3 acceleration = Vector3.zero;
            if (wells == null)
            {
                return acceleration;
            }

            for (int i = 0; i < wells.Count; i++)
            {
                GravityWell well = wells[i];
                if (well == null || well.Mass <= 0f)
                {
                    continue;
                }

                Vector3 toWell = well.transform.position - position;
                // Clamp minimum distance to max(SchwarzschildRadius, PhysicalRadius)
                // so that inside the planet gravity does not diverge (shell theorem approximation)
                float minRadius = Mathf.Max(
                    well.PhysicalRadius,
                    well.SchwarzschildRadius + PhysicsConstants.IntegrationEpsilon,
                    PhysicsConstants.IntegrationEpsilon
                );
                float sqrMinRadius = minRadius * minRadius;
                float sqrDistance = Mathf.Max(toWell.sqrMagnitude, sqrMinRadius);
                float distance = Mathf.Sqrt(sqrDistance);
                Vector3 direction = toWell / distance;

                float a = PhysicsConstants.GravitationalConstant * well.Mass / sqrDistance;
                acceleration += direction * a;
            }

            return acceleration;
        }

        private static void ResolveSurfaceContacts(ref Vector3 position, ref Vector3 velocity, System.Collections.Generic.IReadOnlyList<GravityWell> wells)
        {
            if (wells == null)
            {
                return;
            }

            for (int i = 0; i < wells.Count; i++)
            {
                GravityWell well = wells[i];
                if (well == null || !well.EnableSurfaceCollision)
                {
                    continue;
                }

                float contactRadius = Mathf.Max(well.PhysicalRadius, well.SchwarzschildRadius + PhysicsConstants.IntegrationEpsilon);
                if (contactRadius <= PhysicsConstants.IntegrationEpsilon)
                {
                    continue;
                }

                Vector3 fromCenter = position - well.transform.position;
                float distance = fromCenter.magnitude;
                if (distance >= contactRadius)
                {
                    continue;
                }

                Vector3 normal = distance > PhysicsConstants.IntegrationEpsilon
                    ? fromCenter / distance
                    : Vector3.up;

                position = well.transform.position + normal * contactRadius;

                float radialSpeed = Vector3.Dot(velocity, normal);
                if (radialSpeed < 0f)
                {
                    float bounce = -radialSpeed * PhysicsConstants.SurfaceBounceFactor;
                    velocity -= radialSpeed * normal;
                    velocity += bounce * normal;

                    Vector3 tangential = velocity - Vector3.Dot(velocity, normal) * normal;
                    tangential *= PhysicsConstants.SurfaceTangentialDamping;
                    velocity = tangential + Vector3.Dot(velocity, normal) * normal;
                }
            }
        }

        private static Vector3 CartesianToSpherical(Vector3 p)
        {
            float r = p.magnitude;
            if (r <= PhysicsConstants.IntegrationEpsilon)
            {
                return Vector3.zero;
            }

            float theta = Mathf.Acos(Mathf.Clamp(p.y / r, -1f, 1f));
            float phi = Mathf.Atan2(p.z, p.x);
            return new Vector3(r, theta, phi);
        }
    }
}
