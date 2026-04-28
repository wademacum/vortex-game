using NUnit.Framework;
using UnityEngine;
using Vortex.Ship;
using Vortex.Physics;
using Vortex.Procedural;

namespace Vortex.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for behaviours that require Awake / OnEnable to fire.
    /// In PlayMode, AddComponent and new GameObject() trigger MonoBehaviour lifecycle
    /// synchronously (Awake fires immediately), so no frame yielding is needed.
    /// Tests that require FixedUpdate are validated via direct state manipulation.
    /// </summary>
    public sealed class PhysicsPlayModeTests
    {
        // ── helpers ──────────────────────────────────────────────────────────

        private static GravityWell CreateWell(float mass, float physicalRadius, Vector3 position = default)
        {
            GameObject go = new GameObject("Well");
            go.transform.position = position;
            GravityWell well = go.AddComponent<GravityWell>();
            well.ApplyProceduralBody(mass, physicalRadius);
            return well;
        }

        private static RelativisticBody CreateBody(Vector3 position = default)
        {
            GameObject go = new GameObject("Body");
            go.transform.position = position;
            return go.AddComponent<RelativisticBody>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(go);
            }
        }

        // ── GravityWell lifecycle (Awake/OnEnable fire on AddComponent) ───────

        [Test]
        public void GravityWell_OnEnable_RegistersInRegistry()
        {
            int countBefore = GravityWellRegistry.GetAll().Count;
            CreateWell(88200f, 200f);
            // Awake + OnEnable fire immediately via AddComponent in PlayMode
            Assert.AreEqual(countBefore + 1, GravityWellRegistry.GetAll().Count);
        }

        [Test]
        public void GravityWell_OnDisable_UnregistersFromRegistry()
        {
            GravityWell well = CreateWell(88200f, 200f);
            int countAfterSpawn = GravityWellRegistry.GetAll().Count;

            well.gameObject.SetActive(false);

            Assert.AreEqual(countAfterSpawn - 1, GravityWellRegistry.GetAll().Count);
        }

        [Test]
        public void GravityWell_DestroyImmediate_UnregistersFromRegistry()
        {
            GravityWell well = CreateWell(88200f, 200f);
            int countAfterSpawn = GravityWellRegistry.GetAll().Count;

            Object.DestroyImmediate(well.gameObject);

            Assert.AreEqual(countAfterSpawn - 1, GravityWellRegistry.GetAll().Count);
        }

        // ── RelativisticBody Awake ────────────────────────────────────────────

        [Test]
        public void RelativisticBody_DisablesRigidbodyOnAwake()
        {
            GameObject go = new GameObject("BodyWithRb");
            Rigidbody rb = go.AddComponent<Rigidbody>();
            // Awake fires when RelativisticBody is added
            go.AddComponent<RelativisticBody>();

            Assert.IsTrue(rb.isKinematic, "Rigidbody should be set kinematic by RelativisticBody.Awake()");
            Assert.IsFalse(rb.useGravity, "Rigidbody should have useGravity disabled");
        }

        [Test]
        public void RelativisticBody_PhysicsPosition_MatchesTransformAfterAwake()
        {
            Vector3 spawnPos = new Vector3(10f, 20f, 30f);
            RelativisticBody body = CreateBody(spawnPos);

            Assert.AreEqual(spawnPos, body.PhysicsPosition,
                "PhysicsPosition should be initialised from transform.position in Awake.");
        }

        // ── ProperTime / freeze ───────────────────────────────────────────────

        [Test]
        public void RelativisticBody_FrozenBody_LocalDeltaTimeIsZero()
        {
            RelativisticBody body = CreateBody();
            body.FreezeProperTime();
            // SetLocalDeltaTime is called by GeodesicSystem; simulate freeze path
            body.SetLocalDeltaTime(0f);

            Assert.AreEqual(0f, body.LocalDeltaTime);
            Assert.IsTrue(body.IsTimeFrozen);
        }

        [Test]
        public void RelativisticBody_Restore_AfterFreeze_ProperTimeNonZero()
        {
            RelativisticBody body = CreateBody();
            body.FreezeProperTime();
            body.RestoreProperTime();

            Assert.Greater(body.ProperTime, 0f);
            Assert.IsFalse(body.IsTimeFrozen);
        }

        // ── SetPhysicsState ───────────────────────────────────────────────────

        [Test]
        public void RelativisticBody_SetPhysicsState_UpdatesPhysicsPosition()
        {
            RelativisticBody body = CreateBody();
            Vector3 newPos = new Vector3(100f, 0f, 0f);
            body.SetPhysicsState(newPos, Quaternion.identity, Vector4.zero);

            Assert.AreEqual(newPos, body.PhysicsPosition);
        }

        [Test]
        public void ProceduralBodyPhysicsBinder_AppliesRuntimeSpin_ToRelativisticBody()
        {
            GameObject go = new GameObject("SpinningPlanet");
            RuntimeBodyData data = new RuntimeBodyData
            {
                mass = 1000f,
                radius = 50f,
                rotationSpeed = 24f
            };

            ProceduralBodyPhysicsBinder.Apply(go, data);

            RelativisticBody body = go.GetComponent<RelativisticBody>();
            Assert.IsNotNull(body);
            Assert.AreEqual(Vector3.up * 24f, body.IntrinsicAngularVelocityDegPerSec);
        }

        // ── Surface collision resolution ──────────────────────────────────────

        [Test]
        public void GravityWell_SurfaceCollision_ResolvedPosition_IsOutsidePhysicalRadius()
        {
            GravityWell well = CreateWell(88200f, 100f);
            Vector3 insidePoint = new Vector3(20f, 0f, 0f);

            bool hit = well.TryResolveSurfaceContact(insidePoint, out Vector3 resolved, out _);

            Assert.IsTrue(hit);
            float dist = Vector3.Distance(resolved, well.transform.position);
            Assert.GreaterOrEqual(dist, 100f - 0.01f,
                $"Resolved position must be at or beyond physicalRadius (100). Got: {dist:F3}");
        }

        [Test]
        public void GravityWell_SurfaceCollision_Normal_PointsOutward()
        {
            GravityWell well = CreateWell(88200f, 100f);
            Vector3 insidePoint = new Vector3(20f, 0f, 0f);

            well.TryResolveSurfaceContact(insidePoint, out _, out Vector3 normal);

            float dot = Vector3.Dot(normal, insidePoint.normalized);
            Assert.Greater(dot, 0f, "Surface normal should point outward from the well centre.");
        }

        [Test]
        public void GravityWell_SurfaceCollision_OutsidePoint_ReturnsFalse()
        {
            GravityWell well = CreateWell(88200f, 100f);
            Vector3 outsidePoint = new Vector3(500f, 0f, 0f);

            bool hit = well.TryResolveSurfaceContact(outsidePoint, out _, out _);

            Assert.IsFalse(hit);
        }

        // ── GravityWellData snapshot ──────────────────────────────────────────

        [Test]
        public void GravityWell_ToData_CapturesCurrentWorldPosition()
        {
            GravityWell well = CreateWell(88200f, 200f);
            well.transform.position = new Vector3(5f, 10f, 15f);

            GravityWellData data = well.ToData();

            Assert.AreEqual(5f, data.position.x, 0.001f);
            Assert.AreEqual(10f, data.position.y, 0.001f);
            Assert.AreEqual(15f, data.position.z, 0.001f);
        }

        // ── Ship controller thrust integration (Phase 1 P1 / Task 8) ───────

        [Test]
        public void ShipController_ApplyThrustInput_ChangesFourVelocity_WhenProperTimeActive()
        {
            GameObject go = new GameObject("Ship");
            RelativisticBody body = go.AddComponent<RelativisticBody>();
            ShipController controller = go.AddComponent<ShipController>();

            Vector4 before = body.FourVelocity;
            controller.ApplyThrustInput(new Vector3(0f, 0f, 1f), 1f);
            Vector4 after = body.FourVelocity;

            Assert.Greater(after.z, before.z, "Forward thrust should increase z velocity component.");
        }

        [Test]
        public void ShipController_ApplyThrustInput_DoesNothing_WhenProperTimeFrozen()
        {
            GameObject go = new GameObject("ShipFrozen");
            RelativisticBody body = go.AddComponent<RelativisticBody>();
            ShipController controller = go.AddComponent<ShipController>();

            body.FreezeProperTime();
            Vector4 before = body.FourVelocity;
            controller.ApplyThrustInput(new Vector3(0f, 0f, 1f), 1f);
            Vector4 after = body.FourVelocity;

            Assert.AreEqual(before.x, after.x, 0.0001f);
            Assert.AreEqual(before.y, after.y, 0.0001f);
            Assert.AreEqual(before.z, after.z, 0.0001f);
            Assert.AreEqual(before.w, after.w, 0.0001f);
        }
    }
}
