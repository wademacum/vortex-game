using System;
using UnityEngine;

namespace Vortex.Procedural
{
    public readonly struct VoxelSdfDistribution
    {
        public readonly int negativeCount;
        public readonly int positiveCount;
        public readonly float minValue;
        public readonly float maxValue;

        public VoxelSdfDistribution(int negativeCount, int positiveCount, float minValue, float maxValue)
        {
            this.negativeCount = negativeCount;
            this.positiveCount = positiveCount;
            this.minValue = minValue;
            this.maxValue = maxValue;
        }
    }

    public sealed class VoxelDataManager : MonoBehaviour
    {
        [Header("Compute")]
        [SerializeField] private ComputeShader voxelDataGenerator;
        [SerializeField] private bool autoAssignGeneratorInEditor = true;
        [SerializeField, Min(8)] private int gridResolution = 64;
        [SerializeField, Min(0.001f)] private float voxelSize = 1f;

        private const string KernelName = "CSMain";
        private const int ThreadsPerAxis = 8;

        private int kernelIndex = -1;
        private ComputeBuffer sdfBuffer;
        private float[] cpuReadback;
        private bool hasLoggedMissingGenerator;
        private bool hasLoggedMissingKernel;

        #if UNITY_EDITOR
        private bool playModeHookRegistered;
        #endif

        public int GridResolution => gridResolution;
        public float VoxelSize => voxelSize;
        public int VoxelCount => gridResolution * gridResolution * gridResolution;
        public ComputeBuffer SdfBuffer => sdfBuffer;
        public bool HasGenerator => voxelDataGenerator != null;

        public void ConfigureGrid(int resolution, float size)
        {
            int clampedResolution = Mathf.Max(8, resolution);
            float clampedSize = Mathf.Max(0.001f, size);
            if (clampedResolution == gridResolution && Mathf.Approximately(clampedSize, voxelSize))
            {
                return;
            }

            gridResolution = clampedResolution;
            voxelSize = clampedSize;
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();

            #if UNITY_EDITOR
            RegisterPlayModeHook();
            #endif
        }

        private void OnDisable()
        {
            #if UNITY_EDITOR
            UnregisterPlayModeHook();
            #endif

            ReleaseBuffers();
        }

        private void OnDestroy()
        {
            ReleaseBuffers();
        }

        private void OnApplicationQuit()
        {
            ReleaseBuffers();
        }

        public void EnsureInitialized()
        {
            TryResolveGenerator();

            if (voxelDataGenerator == null)
            {
                LogMissingGeneratorOnce();
                return;
            }

            if (kernelIndex < 0)
            {
                if (!voxelDataGenerator.HasKernel(KernelName))
                {
                    LogMissingKernelOnce();
                    return;
                }

                kernelIndex = voxelDataGenerator.FindKernel(KernelName);
            }

            int voxelCount = VoxelCount;
            if (sdfBuffer != null && sdfBuffer.count == voxelCount)
            {
                return;
            }

            ReleaseBuffers();
            sdfBuffer = new ComputeBuffer(voxelCount, sizeof(float));
            cpuReadback = new float[voxelCount];
        }

        public bool Generate(RuntimeBodyData bodyData, Vector3 gridOrigin)
        {
            EnsureInitialized();

            if (voxelDataGenerator != null && !voxelDataGenerator.HasKernel(KernelName))
            {
                kernelIndex = -1;
                LogMissingKernelOnce();
                return false;
            }

            if (voxelDataGenerator != null && kernelIndex < 0 && voxelDataGenerator.HasKernel(KernelName))
            {
                kernelIndex = voxelDataGenerator.FindKernel(KernelName);
            }

            if (!IsReady())
            {
                if (voxelDataGenerator == null)
                {
                    LogMissingGeneratorOnce();
                }
                else
                {
                    LogMissingKernelOnce();
                }

                return false;
            }

            PushCommonParams(bodyData, gridOrigin);
            PushNoiseLayer("Continent", bodyData.noiseLayerConfig.continent);
            PushNoiseLayer("Mountain", bodyData.noiseLayerConfig.mountain);
            PushNoiseLayer("Detail", bodyData.noiseLayerConfig.detail);

            voxelDataGenerator.SetBuffer(kernelIndex, "_SdfOutput", sdfBuffer);

            int groups = Mathf.CeilToInt(gridResolution / (float)ThreadsPerAxis);
            voxelDataGenerator.Dispatch(kernelIndex, groups, groups, groups);
            return true;
        }

        public VoxelSdfDistribution ReadbackDistribution()
        {
            if (!IsReady())
            {
                throw new InvalidOperationException("SDF buffer is not ready.");
            }

            sdfBuffer.GetData(cpuReadback);

            int negative = 0;
            int positive = 0;
            float min = float.MaxValue;
            float max = float.MinValue;

            for (int i = 0; i < cpuReadback.Length; i++)
            {
                float v = cpuReadback[i];
                if (v < 0f)
                {
                    negative++;
                }
                else
                {
                    positive++;
                }

                if (v < min)
                {
                    min = v;
                }

                if (v > max)
                {
                    max = v;
                }
            }

            return new VoxelSdfDistribution(negative, positive, min, max);
        }

        private bool IsReady()
        {
            return voxelDataGenerator != null && kernelIndex >= 0 && sdfBuffer != null;
        }

        private void PushCommonParams(RuntimeBodyData bodyData, Vector3 gridOrigin)
        {
            voxelDataGenerator.SetInt("_GridResolution", gridResolution);
            voxelDataGenerator.SetFloat("_VoxelSize", voxelSize);
            voxelDataGenerator.SetVector("_GridOrigin", new Vector4(gridOrigin.x, gridOrigin.y, gridOrigin.z, 0f));
            voxelDataGenerator.SetFloat("_BaseRadius", Mathf.Max(0f, bodyData.radius));
        }

        private void PushNoiseLayer(string prefix, NoiseLayer layer)
        {
            voxelDataGenerator.SetFloat($"_{prefix}Scale", Mathf.Max(0f, layer.scale));
            voxelDataGenerator.SetInt($"_{prefix}Octaves", Mathf.Max(0, layer.octaves));
            voxelDataGenerator.SetFloat($"_{prefix}Amplitude", Mathf.Max(0f, layer.amplitude));
            voxelDataGenerator.SetFloat($"_{prefix}Persistence", Mathf.Max(0f, layer.persistence));
            voxelDataGenerator.SetFloat($"_{prefix}Lacunarity", Mathf.Max(1f, layer.lacunarity));
            voxelDataGenerator.SetVector($"_{prefix}Offset", new Vector4(layer.offset.x, layer.offset.y, layer.offset.z, 0f));
        }

        private void ReleaseBuffers()
        {
            if (sdfBuffer != null)
            {
                sdfBuffer.Release();
                sdfBuffer.Dispose();
                sdfBuffer = null;
            }

            cpuReadback = null;
            kernelIndex = -1;
        }

        public void ReleaseResources()
        {
            ReleaseBuffers();
        }

        #if UNITY_EDITOR
        private void RegisterPlayModeHook()
        {
            if (playModeHookRegistered)
            {
                return;
            }

            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            UnityEditor.EditorApplication.quitting += OnEditorQuitting;
            playModeHookRegistered = true;
        }

        private void UnregisterPlayModeHook()
        {
            if (!playModeHookRegistered)
            {
                return;
            }

            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            UnityEditor.EditorApplication.quitting -= OnEditorQuitting;
            playModeHookRegistered = false;
        }

        private void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode ||
                state == UnityEditor.PlayModeStateChange.ExitingEditMode)
            {
                ReleaseBuffers();
            }
        }

        private void OnBeforeAssemblyReload()
        {
            ReleaseBuffers();
        }

        private void OnEditorQuitting()
        {
            ReleaseBuffers();
        }
        #endif

        private void TryResolveGenerator()
        {
            if (voxelDataGenerator != null)
            {
                return;
            }

            #if UNITY_EDITOR
            if (autoAssignGeneratorInEditor)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("VoxelDataGenerator t:ComputeShader");
                if (guids != null && guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    voxelDataGenerator = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(path);
                }
            }
            #endif
        }

        private void LogMissingGeneratorOnce()
        {
            if (hasLoggedMissingGenerator)
            {
                return;
            }

            hasLoggedMissingGenerator = true;
            Debug.LogWarning("[VoxelDataManager] VoxelDataGenerator.compute is not assigned. Assign it on the component or keep auto-assign enabled.", this);
        }

        private void LogMissingKernelOnce()
        {
            if (hasLoggedMissingKernel)
            {
                return;
            }

            hasLoggedMissingKernel = true;
            Debug.LogWarning($"[VoxelDataManager] Kernel '{KernelName}' not found in assigned compute shader. Check shader compile errors.", this);
        }
    }
}
