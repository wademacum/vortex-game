using UnityEngine;
using Vortex.Physics;
using Vortex.Debugging;

namespace Vortex.Test
{
    /// <summary>
    /// Görev 7 Test Kurulumu - Geodezik Yörünge Doğrulama
    /// </summary>
    public static class TestSetup_Görev7
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializeTestScene()
        {
            // Sahne başladığında otomatik olarak test ortamını kur
            if (Application.isPlaying)
            {
                SetupTestEnvironment();
            }
        }

        public static void SetupTestEnvironment()
        {
            // GeodesicTestHarness'ı bul
            var harness = Object.FindFirstObjectByType<GeodesicTestHarness>();
            if (harness != null)
            {
                UnityEngine.Debug.Log("[Görev 7 Setup] Setting up single orbit body...");
                harness.SetupSingleOrbitBody();
            }

            // GeodesicOrbitTestRunner'ı bul
            var runner = Object.FindFirstObjectByType<GeodesicOrbitTestRunner>();
            if (runner != null)
            {
                UnityEngine.Debug.Log("[Görev 7 Setup] Applying low-speed scenario...");
                runner.ApplyLowSpeedScenario();
            }

            UnityEngine.Debug.Log("[Görev 7 Setup] Test environment ready!");
        }

        public static void SpawnStressBodies(int count = 50)
        {
            var harness = Object.FindFirstObjectByType<GeodesicTestHarness>();
            if (harness != null)
            {
                UnityEngine.Debug.Log("[Görev 7 Setup] Spawning stress bodies...");
                harness.SpawnStressBodies();
            }
        }

        public static void ApplyHighSpeedScenario()
        {
            var runner = Object.FindFirstObjectByType<GeodesicOrbitTestRunner>();
            if (runner != null)
            {
                UnityEngine.Debug.Log("[Görev 7 Setup] Applying high-speed scenario...");
                runner.ApplyHighSpeedScenario();
            }
        }

        public static void RunFreezeTest()
        {
            var runner = Object.FindFirstObjectByType<GeodesicOrbitTestRunner>();
            if (runner != null)
            {
                UnityEngine.Debug.Log("[Görev 7 Setup] Running freeze ProperTime check...");
                runner.RunFreezeProperTimeCheck();
            }
        }
    }
}
