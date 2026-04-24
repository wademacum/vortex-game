using UnityEngine;

namespace Vortex.Physics
{
    public static class GeodesicIntegrator
    {
        public static void Integrate(
            RelativisticBody body,
            System.Collections.Generic.IReadOnlyList<GravityWell> wells,
            System.Collections.Generic.IReadOnlyList<RelativisticBody> bodies,
            float globalDeltaTime)
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
            float grFactor = ComputeGrFactor(body, body.PhysicsPosition, wells, bodies);
            float localDeltaTime = globalDeltaTime * properTime * srFactor * grFactor * softFactor;
            body.SetLocalDeltaTime(localDeltaTime);

            if (localDeltaTime <= 0f)
            {
                return;
            }

            float maxSubstep = Mathf.Max(PhysicsConstants.IntegrationEpsilon, PhysicsConstants.MaxIntegrationSubstep);
            int substepCount = Mathf.Clamp(Mathf.CeilToInt(localDeltaTime / maxSubstep), 1, 128);
            float substepDt = localDeltaTime / substepCount;

            Vector3 integratedPosition = body.PhysicsPosition;
            Quaternion integratedRotation = body.PhysicsRotation;
            Vector3 integratedAngularVelocity = body.AngularVelocityDegPerSec;
            for (int step = 0; step < substepCount; step++)
            {
                IntegrateRk4(body, ref integratedPosition, ref velocity, ref integratedRotation, ref integratedAngularVelocity, wells, bodies, substepDt);
            }

            Vector4 nextFourVelocity = body.FourVelocity;
            nextFourVelocity.x = velocity.x;
            nextFourVelocity.y = velocity.y;
            nextFourVelocity.z = velocity.z;
            body.SetPhysicsState(integratedPosition, integratedRotation, nextFourVelocity);
            body.SetAngularVelocity(integratedAngularVelocity);
        }

        private static void IntegrateRk4(
            RelativisticBody body,
            ref Vector3 position,
            ref Vector3 velocity,
            ref Quaternion rotation,
            ref Vector3 angularVelocity,
            System.Collections.Generic.IReadOnlyList<GravityWell> wells,
            System.Collections.Generic.IReadOnlyList<RelativisticBody> bodies,
            float dt)
        {
            Vector3 p0 = position;
            Vector3 v0 = velocity;
            Quaternion q0 = rotation;

            Vector3 a1 = ComputeAcceleration(body, p0, wells, bodies);
            Vector3 k1v = a1 * dt;
            Vector3 k1p = v0 * dt;

            Vector3 a2 = ComputeAcceleration(body, p0 + 0.5f * k1p, wells, bodies);
            Vector3 k2v = a2 * dt;
            Vector3 k2p = (v0 + 0.5f * k1v) * dt;

            Vector3 a3 = ComputeAcceleration(body, p0 + 0.5f * k2p, wells, bodies);
            Vector3 k3v = a3 * dt;
            Vector3 k3p = (v0 + 0.5f * k2v) * dt;

            Vector3 a4 = ComputeAcceleration(body, p0 + k3p, wells, bodies);
            Vector3 k4v = a4 * dt;
            Vector3 k4p = (v0 + k3v) * dt;

            Vector3 nextVelocity = v0 + (k1v + 2f * k2v + 2f * k3v + k4v) / 6f;
            Vector3 nextPosition = p0 + (k1p + 2f * k2p + 2f * k3p + k4p) / 6f;
            Quaternion nextRotation = q0;

            bool hadSurfaceContact = ResolveSurfaceContacts(body, ref nextPosition, ref nextVelocity, ref nextRotation, ref angularVelocity, wells, dt);

            if (angularVelocity.sqrMagnitude > PhysicsConstants.IntegrationEpsilon)
            {
                float stepDegrees = angularVelocity.magnitude * dt;
                Vector3 axis = angularVelocity.normalized;
                nextRotation = Quaternion.AngleAxis(stepDegrees, axis) * nextRotation;
            }

            float dampingPerSecond = Mathf.Pow(Mathf.Clamp01(body.AngularDampingPerFixedStep), 1f / Mathf.Max(PhysicsConstants.IntegrationEpsilon, Time.fixedDeltaTime));
            angularVelocity *= Mathf.Pow(dampingPerSecond, dt);

            if (hadSurfaceContact)
            {
                float contactAngularDamping = Mathf.Exp(-PhysicsConstants.SurfaceAngularFrictionPerSecond * dt);
                angularVelocity *= contactAngularDamping;

                // If the body has effectively settled on the surface, kill residual spin.
                if (nextVelocity.magnitude < PhysicsConstants.SurfaceRestLinearSpeed &&
                    angularVelocity.magnitude < PhysicsConstants.SurfaceRestAngularSpeed)
                {
                    nextVelocity = Vector3.zero;
                    angularVelocity = Vector3.zero;
                }
            }

            float maxAngularSpeed = PhysicsConstants.MaxAngularSpeedDegPerSec;
            if (angularVelocity.magnitude > maxAngularSpeed)
            {
                angularVelocity = angularVelocity.normalized * maxAngularSpeed;
            }

            float hardSpeed = PhysicsConstants.HardSpeedLimitRatio * PhysicsConstants.SpeedOfLight;
            if (nextVelocity.magnitude > hardSpeed && nextVelocity.sqrMagnitude > PhysicsConstants.IntegrationEpsilon)
            {
                nextVelocity = nextVelocity.normalized * hardSpeed;
            }

            // Rotation can push corners back into the surface; depenetrate a few times.
            SolveResidualPenetration(body, ref nextPosition, nextRotation, wells);

            position = nextPosition;
            velocity = nextVelocity;
            rotation = nextRotation;
        }

        private static float ComputeGrFactor(
            RelativisticBody targetBody,
            Vector3 position,
            System.Collections.Generic.IReadOnlyList<GravityWell> wells,
            System.Collections.Generic.IReadOnlyList<RelativisticBody> bodies)
        {
            if ((wells == null || wells.Count == 0) && (bodies == null || bodies.Count == 0))
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

            if (bodies != null)
            {
                for (int i = 0; i < bodies.Count; i++)
                {
                    RelativisticBody sourceBody = bodies[i];
                    if (sourceBody == null || sourceBody == targetBody || !sourceBody.ContributesToGravity)
                    {
                        continue;
                    }

                    float sourceRs = 2f * PhysicsConstants.GravitationalConstant * sourceBody.GravitationalMass /
                        (PhysicsConstants.SpeedOfLight * PhysicsConstants.SpeedOfLight);
                    float r = Vector3.Distance(position, sourceBody.PhysicsPosition);
                    float minRadius = Mathf.Max(sourceRs + PhysicsConstants.IntegrationEpsilon, PhysicsConstants.IntegrationEpsilon);
                    r = Mathf.Max(r, minRadius);
                    float term = 1f - (sourceRs / r);
                    float factor = Mathf.Sqrt(Mathf.Max(PhysicsConstants.MinRelativityFactor, term));
                    if (factor < minFactor)
                    {
                        minFactor = factor;
                    }
                }
            }

            return minFactor;
        }

        private static Vector3 ComputeAcceleration(
            RelativisticBody targetBody,
            Vector3 position,
            System.Collections.Generic.IReadOnlyList<GravityWell> wells,
            System.Collections.Generic.IReadOnlyList<RelativisticBody> bodies)
        {
            Vector3 acceleration = Vector3.zero;
            if (wells == null && bodies == null)
            {
                return acceleration;
            }

            if (wells != null)
            {
                for (int i = 0; i < wells.Count; i++)
                {
                    GravityWell well = wells[i];
                    if (well == null || well.Mass <= 0f)
                    {
                        continue;
                    }

                    Vector3 toWell = well.transform.position - position;
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
            }

            if (bodies != null)
            {
                for (int i = 0; i < bodies.Count; i++)
                {
                    RelativisticBody sourceBody = bodies[i];
                    if (sourceBody == null || sourceBody == targetBody || !sourceBody.ContributesToGravity)
                    {
                        continue;
                    }

                    Vector3 toSource = sourceBody.PhysicsPosition - position;
                    float sourceRadius = ResolveBodyInteractionRadius(sourceBody);
                    float sqrMinRadius = sourceRadius * sourceRadius;
                    float sqrDistance = Mathf.Max(toSource.sqrMagnitude, sqrMinRadius);
                    float distance = Mathf.Sqrt(sqrDistance);
                    Vector3 direction = toSource / distance;

                    float a = PhysicsConstants.GravitationalConstant * sourceBody.GravitationalMass / sqrDistance;
                    acceleration += direction * a;
                }
            }

            return acceleration;
        }

        private static float ResolveBodyInteractionRadius(RelativisticBody body)
        {
            Collider collider = body.BodyCollider;
            if (collider == null)
            {
                return 0.5f;
            }

            Vector3 extents = collider.bounds.extents;
            return Mathf.Max(extents.x, extents.y, extents.z, PhysicsConstants.IntegrationEpsilon);
        }

        private static void SolveResidualPenetration(
            RelativisticBody body,
            ref Vector3 position,
            Quaternion rotation,
            System.Collections.Generic.IReadOnlyList<GravityWell> wells)
        {
            if (wells == null)
            {
                return;
            }

            Collider bodyCollider = body.BodyCollider;
            if (bodyCollider == null)
            {
                return;
            }

            for (int iter = 0; iter < 4; iter++)
            {
                bool moved = false;
                for (int i = 0; i < wells.Count; i++)
                {
                    GravityWell well = wells[i];
                    if (well == null || !well.EnableSurfaceCollision)
                    {
                        continue;
                    }

                    if (well.TryResolveSurfaceContact(bodyCollider, rotation, position, out Vector3 resolvedPosition, out _))
                    {
                        position = resolvedPosition;
                        moved = true;
                    }
                }

                if (!moved)
                {
                    break;
                }
            }
        }

        private static bool ResolveSurfaceContacts(RelativisticBody body, ref Vector3 position, ref Vector3 velocity, ref Quaternion rotation, ref Vector3 angularVelocity, System.Collections.Generic.IReadOnlyList<GravityWell> wells, float dt)
        {
            if (wells == null)
            {
                return false;
            }

            Collider bodyCollider = body.BodyCollider;
            bool hadContact = false;

            for (int i = 0; i < wells.Count; i++)
            {
                GravityWell well = wells[i];
                if (well == null || !well.EnableSurfaceCollision)
                {
                    continue;
                }

                if (!well.TryResolveSurfaceContact(bodyCollider, rotation, position, out Vector3 resolvedPosition, out Vector3 normal))
                {
                    continue;
                }

                hadContact = true;
                position = resolvedPosition;

                float radialSpeed = Vector3.Dot(velocity, normal);
                if (radialSpeed < 0f)
                {
                    float inwardSpeed = -radialSpeed;
                    float bounce = inwardSpeed > PhysicsConstants.SurfaceNoBounceSpeed
                        ? inwardSpeed * PhysicsConstants.SurfaceBounceFactor
                        : 0f;
                    velocity -= radialSpeed * normal;
                    velocity += bounce * normal;

                    Vector3 tangential = velocity - Vector3.Dot(velocity, normal) * normal;
                    float surfaceFriction = Mathf.Exp(-PhysicsConstants.SurfaceDynamicFrictionPerSecond * dt);
                    tangential *= surfaceFriction;
                    tangential *= PhysicsConstants.SurfaceTangentialDamping;
                    if (tangential.magnitude < PhysicsConstants.SurfaceStaticFrictionSpeed)
                    {
                        tangential = Vector3.zero;
                    }
                    velocity = tangential + Vector3.Dot(velocity, normal) * normal;

                    if (inwardSpeed > PhysicsConstants.SurfaceStickReleaseSpeed && tangential.sqrMagnitude > PhysicsConstants.IntegrationEpsilon)
                    {
                        Vector3 spinAxis = Vector3.Cross(normal, tangential.normalized);
                        if (spinAxis.sqrMagnitude > PhysicsConstants.IntegrationEpsilon)
                        {
                            float spinImpulse =
                                (tangential.magnitude + inwardSpeed) * body.CollisionSpinImpulseScale * body.CollisionSpinScale;
                            float invInertialMass = 1f / Mathf.Max(body.InertialMass, PhysicsConstants.IntegrationEpsilon);
                            angularVelocity += spinAxis.normalized * spinImpulse * invInertialMass;

                            float immediateSpin = tangential.magnitude * PhysicsConstants.CollisionSpinDegreesPerSpeed * dt;
                            immediateSpin = Mathf.Min(immediateSpin, PhysicsConstants.MaxCollisionSpinPerStep);
                            rotation = Quaternion.AngleAxis(immediateSpin, spinAxis.normalized) * rotation;
                        }
                    }
                }

                float normalSpeed = Vector3.Dot(velocity, normal);
                if (normalSpeed > 0f && normalSpeed < PhysicsConstants.SurfaceStickReleaseSpeed)
                {
                    velocity -= normalSpeed * normal;
                }

                // Full rest lock when the body is effectively settled.
                if (velocity.magnitude < PhysicsConstants.SurfaceRestLinearSpeed)
                {
                    velocity = Vector3.zero;
                }
            }

            return hadContact;
        }
    }
}
