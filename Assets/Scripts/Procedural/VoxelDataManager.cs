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

        private const string PlanetKernelName = "CSPlanetVoxel";
        private const string MoonKernelName = "CSMoonVoxel";
        private const string GenericKernelName = "CSPlanetVoxel";
        private const int ThreadsPerAxis = 8;

        private int planetKernelIndex = -1;
        private int moonKernelIndex = -1;
        private int genericKernelIndex = -1;
        private ComputeBuffer sdfBuffer;
        private ComputeBuffer composeBuffer;
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

            if (!ResolveKernelIndices())
            {
                LogMissingKernelOnce();
                return;
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

        public bool Generate(RuntimeBodyData bodyData, Vector3 gridOrigin, SdfComposeCommand[] composeCommands = null)
        {
            EnsureInitialized();
            if (!ResolveKernelIndices())
            {
                LogMissingKernelOnce();
                return false;
            }

            int kernelIndex = ResolveKernelIndex(bodyData.shapeModel);
            if (kernelIndex < 0 || sdfBuffer == null)
            {
                return false;
            }

            PushCommonParams(kernelIndex, bodyData, gridOrigin);
            PushPlanetShapeParams(kernelIndex, bodyData.planetShapeConfig, bodyData.noiseLayerConfig);
            PushMoonShapeParams(kernelIndex, bodyData.moonShapeConfig);
            PushComposeCommands(kernelIndex, composeCommands);

            voxelDataGenerator.SetBuffer(kernelIndex, "_SdfOutput", sdfBuffer);

            int groups = Mathf.CeilToInt(gridResolution / (float)ThreadsPerAxis);
            voxelDataGenerator.Dispatch(kernelIndex, groups, groups, groups);
            return true;
        }

        public VoxelSdfDistribution ReadbackDistribution()
        {
            if (sdfBuffer == null)
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

        private bool ResolveKernelIndices()
        {
            if (voxelDataGenerator == null)
            {
                return false;
            }

            if (planetKernelIndex >= 0 && moonKernelIndex >= 0 && genericKernelIndex >= 0)
            {
                return true;
            }

            if (!voxelDataGenerator.HasKernel(PlanetKernelName) ||
                !voxelDataGenerator.HasKernel(MoonKernelName) ||
                !voxelDataGenerator.HasKernel(GenericKernelName))
            {
                return false;
            }

            planetKernelIndex = voxelDataGenerator.FindKernel(PlanetKernelName);
            moonKernelIndex = voxelDataGenerator.FindKernel(MoonKernelName);
            genericKernelIndex = voxelDataGenerator.FindKernel(GenericKernelName);
            return true;
        }

        private int ResolveKernelIndex(ShapeModel shapeModel)
        {
            switch (shapeModel)
            {
                case ShapeModel.Moon:
                    return moonKernelIndex;
                case ShapeModel.Planet:
                    return planetKernelIndex;
                default:
                    return genericKernelIndex;
            }
        }

        private void PushCommonParams(int kernelIndex, RuntimeBodyData bodyData, Vector3 gridOrigin)
        {
            voxelDataGenerator.SetInt("_GridResolution", gridResolution);
            voxelDataGenerator.SetFloat("_VoxelSize", voxelSize);
            voxelDataGenerator.SetVector("_GridOrigin", new Vector4(gridOrigin.x, gridOrigin.y, gridOrigin.z, 0f));
            voxelDataGenerator.SetFloat("_BaseRadius", Mathf.Max(0f, bodyData.radius));
            voxelDataGenerator.SetVector("_BaseCommonOffset", new Vector4(
                bodyData.baseShapeConfig.commonOffset.x,
                bodyData.baseShapeConfig.commonOffset.y,
                bodyData.baseShapeConfig.commonOffset.z,
                bodyData.baseShapeConfig.radiusBias));
            voxelDataGenerator.SetFloat("_BaseVerticalSquash", Mathf.Max(0.1f, bodyData.baseShapeConfig.verticalSquash));
        }

        private void PushPlanetShapeParams(int kernelIndex, PlanetShapeConfig config, NoiseLayerConfig fallback)
        {
            NoiseLayer continent = ChooseLayer(config.continent, fallback.continent);
            NoiseLayer mountain = ChooseLayer(config.mountain, fallback.mountain);
            NoiseLayer detail = ChooseLayer(config.detail, fallback.detail);
            NoiseLayer mask = config.mask.scale > 0f ? config.mask : fallback.continent;

            PushNoiseLayer("PlanetContinent", continent);
            PushNoiseLayer("PlanetMountain", mountain);
            PushNoiseLayer("PlanetDetail", detail);
            PushNoiseLayer("PlanetMask", mask);

            voxelDataGenerator.SetFloat("_PlanetOceanDepthMultiplier", Mathf.Max(0f, config.oceanDepthMultiplier));
            voxelDataGenerator.SetFloat("_PlanetOceanFloorDepth", Mathf.Max(0f, config.oceanFloorDepth));
            voxelDataGenerator.SetFloat("_PlanetOceanFloorSmoothing", Mathf.Max(0f, config.oceanFloorSmoothing));
            voxelDataGenerator.SetFloat("_PlanetMountainBlend", Mathf.Max(0.001f, config.mountainBlend));
        }

        private void PushMoonShapeParams(int kernelIndex, MoonShapeConfig config)
        {
            PushNoiseLayer("MoonShape", config.shape);
            PushNoiseLayer("MoonRidgeA", config.ridgeA);
            PushNoiseLayer("MoonRidgeB", config.ridgeB);
            voxelDataGenerator.SetInt("_MoonCraterCount", Mathf.Max(0, config.craterCount));
            voxelDataGenerator.SetVector("_MoonCraterRadiusRange", config.craterRadiusRange);
            voxelDataGenerator.SetFloat("_MoonCraterDepth", Mathf.Max(0f, config.craterDepth));
            voxelDataGenerator.SetFloat("_MoonCraterRimSharpness", Mathf.Max(0.01f, config.craterRimSharpness));
            voxelDataGenerator.SetFloat("_MoonCraterNoiseScale", Mathf.Max(0.001f, config.craterNoiseScale));
        }

        private void PushComposeCommands(int kernelIndex, SdfComposeCommand[] composeCommands)
        {
            int count = composeCommands != null ? composeCommands.Length : 0;
            int bufferCount = Mathf.Max(1, count);
            if (composeBuffer == null || composeBuffer.count != bufferCount)
            {
                if (composeBuffer != null)
                {
                    composeBuffer.Release();
                    composeBuffer.Dispose();
                }

                composeBuffer = new ComputeBuffer(bufferCount, SdfComposeCommand.Stride);
            }

            if (count > 0)
            {
                composeBuffer.SetData(composeCommands);
            }
            else
            {
                composeBuffer.SetData(new[] { default(SdfComposeCommand) });
            }

            voxelDataGenerator.SetBuffer(kernelIndex, "_ComposeCommands", composeBuffer);
            voxelDataGenerator.SetInt("_ComposeCommandCount", count);
        }

        private static NoiseLayer ChooseLayer(NoiseLayer preferred, NoiseLayer fallback)
        {
            return preferred.scale > 0f || preferred.amplitude > 0f ? preferred : fallback;
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

            if (composeBuffer != null)
            {
                composeBuffer.Release();
                composeBuffer.Dispose();
                composeBuffer = null;
            }

            cpuReadback = null;
            planetKernelIndex = -1;
            moonKernelIndex = -1;
            genericKernelIndex = -1;
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
            Debug.LogWarning("[VoxelDataManager] One or more voxel kernels are missing. Check shader compile errors.", this);
        }
    }
}
