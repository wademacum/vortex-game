using NUnit.Framework;
using UnityEngine;
using Vortex.Physics;

namespace Vortex.Tests.Editor
{
    /// <summary>
    /// EditMode tests for GravityWell, GravityWellRegistry and RelativisticBody.
    /// No scene or Play Mode required.
    /// </summary>
    public sealed class PhysicsSystemTests
    {
        // ── helpers ──────────────────────────────────────────────────────────

        private static GravityWell CreateWell(float mass, float physicalRadius)
        {
            GameObject go = new GameObject("Well");
            GravityWell well = go.AddComponent<GravityWell>();
            well.ApplyProceduralBody(mass, physicalRadius);
            return well;
        }

        private static RelativisticBody CreateBody()
        {
            GameObject go = new GameObject("Body");
            return go.AddComponent<RelativisticBody>();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up every GameObject created during the test.
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(go);
            }
        }

        // ── GravityWell ───────────────────────────────────────────────────────

        [Test]
        public void GravityWell_SchwarzschildRadius_IsPositive_ForNonZeroMass()
        {
            GravityWell well = CreateWell(88200f, 500f);
            Assert.Greater(well.SchwarzschildRadius, 0f);
        }

        [Test]
        public void GravityWell_SchwarzschildRadius_IsZero_ForZeroMass()
        {
            GravityWell well = CreateWell(0f, 200f);
            Assert.AreEqual(0f, well.SchwarzschildRadius);
        }

        [Test]
        public void GravityWell_SchwarzschildRadius_NeverExceedsPhysicalRadius()
        {
            // With a huge mass and a tiny body, rs would exceed physicalRadius without capping.
            GravityWell well = CreateWell(1e12f, 10f);
            Assert.LessOrEqual(well.SchwarzschildRadius, well.PhysicalRadius);
        }

        [Test]
        public void GravityWell_ApplyProceduralBody_UpdatesMassAndRadius()
        {
            GravityWell well = CreateWell(1f, 1f);
            well.ApplyProceduralBody(50000f, 300f);
            Assert.AreEqual(50000f, well.Mass);
            Assert.AreEqual(300f, well.PhysicalRadius);
        }

        [Test]
        public void GravityWell_SurfaceCollision_EnabledByDefault()
        {
            GravityWell well = CreateWell(88200f, 200f);
            Assert.IsTrue(well.EnableSurfaceCollision,
                "enableSurfaceCollision should default to true so orbits don't clip through bodies.");
        }

        [Test]
        public void GravityWell_TryResolveSurfaceContact_ReturnsFalse_WhenOutsideRadius()
        {
            GravityWell well = CreateWell(88200f, 100f);
            // Point well outside the physical radius — no contact expected.
            Vector3 outsidePoint = new Vector3(200f, 0f, 0f);
            bool contacted = well.TryResolveSurfaceContact(outsidePoint, out _, out _);
            Assert.IsFalse(contacted);
        }

        [Test]
        public void GravityWell_TryResolveSurfaceContact_ReturnsTrue_WhenInsideRadius()
        {
            GravityWell well = CreateWell(88200f, 100f);
            // Point is inside the physical sphere.
            Vector3 insidePoint = new Vector3(10f, 0f, 0f);
            bool contacted = well.TryResolveSurfaceContact(insidePoint, out Vector3 resolved, out Vector3 normal);
            Assert.IsTrue(contacted);
            // After resolution the point should be at or beyond the surface.
            float distFromCenter = Vector3.Distance(resolved, Vector3.zero);
            Assert.GreaterOrEqual(distFromCenter, 100f - 0.01f);
            Assert.IsTrue(normal.sqrMagnitude > 0f, "Surface normal must not be zero vector.");
        }

        // ── GravityWellRegistry ───────────────────────────────────────────────

        [Test]
        public void GravityWellRegistry_Register_AddsWell()
        {
            int before = GravityWellRegistry.GetAll().Count;
            GravityWell well = CreateWell(1f, 50f);
            GravityWellRegistry.Register(well);
            Assert.AreEqual(before + 1, GravityWellRegistry.GetAll().Count);
            GravityWellRegistry.Unregister(well);
        }

        [Test]
        public void GravityWellRegistry_Register_DoesNotDuplicate()
        {
            GravityWell well = CreateWell(1f, 50f);
            GravityWellRegistry.Register(well);
            int countAfterFirst = GravityWellRegistry.GetAll().Count;
            GravityWellRegistry.Register(well); // second registration
            Assert.AreEqual(countAfterFirst, GravityWellRegistry.GetAll().Count);
            GravityWellRegistry.Unregister(well);
        }

        [Test]
        public void GravityWellRegistry_Unregister_RemovesWell()
        {
            GravityWell well = CreateWell(1f, 50f);
            GravityWellRegistry.Register(well);
            int countAfterAdd = GravityWellRegistry.GetAll().Count;
            GravityWellRegistry.Unregister(well);
            Assert.AreEqual(countAfterAdd - 1, GravityWellRegistry.GetAll().Count);
        }

        [Test]
        public void GravityWellRegistry_Register_NullIsIgnored()
        {
            int before = GravityWellRegistry.GetAll().Count;
            GravityWellRegistry.Register(null);
            Assert.AreEqual(before, GravityWellRegistry.GetAll().Count);
        }

        // ── RelativisticBody ──────────────────────────────────────────────────

        [Test]
        public void RelativisticBody_FreezeProperTime_SetsProperTimeToZero()
        {
            RelativisticBody body = CreateBody();
            body.FreezeProperTime();
            Assert.AreEqual(0f, body.ProperTime);
        }

        [Test]
        public void RelativisticBody_IsTimeFrozen_TrueAfterFreeze()
        {
            RelativisticBody body = CreateBody();
            body.FreezeProperTime();
            Assert.IsTrue(body.IsTimeFrozen);
        }

        [Test]
        public void RelativisticBody_RestoreProperTime_RestoresNonZeroProperTime()
        {
            RelativisticBody body = CreateBody();
            body.FreezeProperTime();
            body.RestoreProperTime();
            Assert.Greater(body.ProperTime, 0f);
        }

        [Test]
        public void RelativisticBody_IsTimeFrozen_FalseAfterRestore()
        {
            RelativisticBody body = CreateBody();
            body.FreezeProperTime();
            body.RestoreProperTime();
            Assert.IsFalse(body.IsTimeFrozen);
        }

        [Test]
        public void RelativisticBody_SetProperTimeScale_ClampsToZeroOne()
        {
            RelativisticBody body = CreateBody();
            body.SetProperTimeScale(5f);
            Assert.AreEqual(1f, body.ProperTime);
            body.SetProperTimeScale(-1f);
            Assert.AreEqual(0f, body.ProperTime);
        }

        [Test]
        public void RelativisticBody_SetLocalDeltaTime_NeverNegative()
        {
            RelativisticBody body = CreateBody();
            body.SetLocalDeltaTime(-10f);
            Assert.AreEqual(0f, body.LocalDeltaTime);
        }

        [Test]
        public void RelativisticBody_ConfigureIntrinsicSpin_UsesFallbackAxis()
        {
            RelativisticBody body = CreateBody();
            body.ConfigureIntrinsicSpin(12f, Vector3.zero);

            Assert.AreEqual(Vector3.up * 12f, body.IntrinsicAngularVelocityDegPerSec);
        }

        // NOTE: RelativisticBody_NoRigidbody_OnAwake is intentionally omitted here.
        // Awake() is not invoked in EditMode NUnit tests; that behaviour is covered
        // by a PlayMode test instead.

        // ── GravityWellData ───────────────────────────────────────────────────

        [Test]
        public void GravityWell_ToData_MatchesWellValues()
        {
            GravityWell well = CreateWell(88200f, 300f);
            well.transform.position = new Vector3(10f, 20f, 30f);
            GravityWellData data = well.ToData();
            Assert.AreEqual(well.Mass, data.mass);
            Assert.AreEqual(well.SchwarzschildRadius, data.schwarzschildRadius);
            Assert.AreEqual(well.PhysicalRadius, data.physicalRadius);
            Assert.AreEqual(10f, data.position.x, 0.001f);
            Assert.AreEqual(20f, data.position.y, 0.001f);
            Assert.AreEqual(30f, data.position.z, 0.001f);
        }
    }
}
