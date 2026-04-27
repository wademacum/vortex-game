using System;
using System.Collections.Generic;
using UnityEngine;
using Vortex.Physics;

namespace Vortex.Procedural
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    [RequireComponent(typeof(VoxelDataManager))]
    [RequireComponent(typeof(MarchingCubesMesher))]
    public sealed class DynamicPlanet : MonoBehaviour
    {
        [Header("Generation Source")]
        [SerializeField] private bool useFactoryGeneration = true;
        [SerializeField] private int seed = 1337;
        [SerializeField] private BodyClass bodyClass = BodyClass.Planet;
        [SerializeField] private CelestialBodyTemplate[] templatePool;
        [SerializeField] private CelestialBodyTemplate selectedTemplate;
        [SerializeField] private bool preferSelectedTemplate = true;
        [SerializeField] private bool randomizeSelectedTemplateRanges = false;

        [Header("Fallback Runtime Data")]
        [SerializeField, Min(1f)] private float fallbackMass = 88200f;
        [SerializeField, Min(1f)] private float fallbackRadius = 220f;

        [Header("LOD")]
        [SerializeField, Min(8)] private int nearResolution = 64;
        [SerializeField, Min(8)] private int midResolution = 32;
        [SerializeField, Min(8)] private int farResolution = 16;
        [SerializeField, Min(1f)] private float nearDistance = 450f;
        [SerializeField, Min(1f)] private float farDistance = 1100f;
        [SerializeField, Min(1f)] private float radiusPaddingMultiplier = 1.35f;

        [Header("Meshing")]
        [SerializeField] private float isoLevel = 0f;
        [SerializeField] private bool regenerateOnStart = true;
        [SerializeField] private bool runtimeLodUpdates = true;
        [SerializeField] private bool generateInEditMode = true;
        [SerializeField, Min(0.1f)] private float retryIntervalSeconds = 0.75f;
        [SerializeField, Min(0.01f)] private float defaultBlendStrength = 6f;

        [Header("Rendering")]
        [SerializeField] private bool forceHdrpLitMaterial = true;

        [SerializeField, HideInInspector] private List<DamageStamp> persistentDamageStamps = new List<DamageStamp>();

        private RuntimeBodyData runtimeData;
        private bool runtimeDataReady;
        private bool hasLoggedFactoryError;
        private bool hasLoggedModeOverride;
        private int lastTemplateSourceHash;
        private int activeResolution = -1;
        private Mesh generatedMesh;
        private float nextRetryTime;
        private bool hasLoggedPipelineWarning;
        private bool hasLoggedMissingRenderShader;
        private Material autoMaterial;
        private bool shapeDirty = true;
        private bool shadingDirty = true;
        private bool composeDirty = true;
        private readonly List<SdfComposeCommand> composeCommandCache = new List<SdfComposeCommand>();

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private VoxelDataManager voxelDataManager;
        private MarchingCubesMesher marchingCubesMesher;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
            voxelDataManager = GetComponent<VoxelDataManager>();
            marchingCubesMesher = GetComponent<MarchingCubesMesher>();
            lastTemplateSourceHash = ComputeTemplateSourceHash();
            EnsureVisibleMaterial();
        }

        private void Start()
        {
            ResolveRuntimeData();
            ApplyRuntimePhysicsBinding();

            if (regenerateOnStart)
            {
                RegenerateMesh();
            }
        }

        private void Update()
        {
            if (HasTemplateSourceChanged())
            {
                MarkRuntimeDataDirty(markShape: true, markShading: true, markCompose: true);
                RegenerateMesh();
                return;
            }

            if (!runtimeLodUpdates && Application.isPlaying)
            {
                return;
            }

            if (!Application.isPlaying && !generateInEditMode)
            {
                return;
            }

            if (!runtimeDataReady)
            {
                ResolveRuntimeData();
            }

            if (!runtimeDataReady || Time.realtimeSinceStartup < nextRetryTime)
            {
                return;
            }

            int desired = ResolveLodResolution();
            if (desired != activeResolution)
            {
                shapeDirty = true;
                shadingDirty = true;
                RegenerateMesh();
            }
            else if (shadingDirty && generatedMesh != null)
            {
                RebuildShadingOnly();
            }
        }

        [ContextMenu("Refresh Runtime Data")]
        public void RefreshRuntimeData()
        {
            MarkRuntimeDataDirty(markShape: true, markShading: true, markCompose: true);
            RegenerateMesh();
        }

        public bool RandomizeSelectedTemplateShape()
        {
            if (selectedTemplate == null)
            {
                Debug.LogWarning("[DynamicPlanet] Random shape requested, but no selected template is assigned.", this);
                return false;
            }

            int randomSeed = unchecked(seed * 486187739 + Environment.TickCount);
            bool changed = CelestialBodyTemplate.Randomize(selectedTemplate, randomSeed);
            if (!changed)
            {
                Debug.LogWarning("[DynamicPlanet] Selected template does not support body-specific randomization.", this);
                return false;
            }

            if (selectedTemplate is PlanetTemplate planetTemplate)
            {
                planetTemplate.ApplyAuthoringDefaults();
            }
            else if (selectedTemplate is MoonTemplate moonTemplate)
            {
                moonTemplate.ApplyAuthoringDefaults();
            }
            /*
            else if (selectedTemplate is AsteroidClusterTemplate asteroidTemplate)
            {
                asteroidTemplate.ApplyAuthoringDefaults();
            }
            */

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(selectedTemplate);
            #endif

            MarkRuntimeDataDirty(markShape: true, markShading: true, markCompose: true);
            RegenerateMesh();
            return true;
        }

        [ContextMenu("Regenerate Mesh")]
        public void RegenerateMesh()
        {
            RegenerateMesh(null, 0f);
        }

        public void RegenerateMesh(DynamicPlanet other, float blendFactor = 0f)
        {
            ResolveRuntimeData();
            if (!runtimeDataReady)
            {
                Debug.LogWarning("[DynamicPlanet] Runtime data unavailable. Skipping mesh regeneration.", this);
                return;
            }

            int resolution = ResolveLodResolution();
            float diameter = Mathf.Max(1f, runtimeData.radius * 2f * radiusPaddingMultiplier);
            diameter = Mathf.Max(diameter, runtimeData.radius * 2f * EstimateRequiredPaddingMultiplier(runtimeData));
            float voxelSize = diameter / Mathf.Max(1, resolution - 1);
            Vector3 gridOrigin = new Vector3(-diameter * 0.5f, -diameter * 0.5f, -diameter * 0.5f);

            RebuildComposeCache(other, blendFactor);

            if (shapeDirty || composeDirty || generatedMesh == null || resolution != activeResolution)
            {
                voxelDataManager.ConfigureGrid(resolution, voxelSize);
                if (!voxelDataManager.Generate(runtimeData, gridOrigin, composeCommandCache.Count > 0 ? composeCommandCache.ToArray() : null))
                {
                    ScheduleRetry("VoxelDataGenerator missing or not initialized.");
                    return;
                }

                Mesh mesh = marchingCubesMesher.GenerateMesh(
                    voxelDataManager.SdfBuffer,
                    resolution,
                    voxelSize,
                    gridOrigin,
                    isoLevel,
                    runtimeData);

                if (mesh == null)
                {
                    ScheduleRetry("MarchingCubesMesher missing or not initialized.");
                    return;
                }

                if (generatedMesh != null)
                {
                    DestroyImmediate(generatedMesh);
                }

                generatedMesh = mesh;
                meshFilter.sharedMesh = generatedMesh;
                meshCollider.sharedMesh = generatedMesh;
                activeResolution = resolution;
                hasLoggedPipelineWarning = false;
                shapeDirty = false;
                shadingDirty = false;
                composeDirty = false;
                EnsureVisibleMaterial();
                ApplyRuntimeMaterialProperties();
                ApplyRuntimePhysicsBinding();
                return;
            }

            if (shadingDirty)
            {
                RebuildShadingOnly();
            }
        }

        public void ApplyDamage(Vector3 worldPos, float radius, float depth)
        {
            Vector3 local = transform.InverseTransformPoint(worldPos);
            DamageStamp stamp = new DamageStamp
            {
                shape = SdfComposeShape.Sphere,
                localPointA = local,
                localPointB = local,
                radius = Mathf.Max(0.01f, radius),
                depth = Mathf.Max(0.001f, depth)
            };

            persistentDamageStamps.Add(stamp);
            composeDirty = true;
            RegenerateMesh();
        }

        private void RebuildShadingOnly()
        {
            if (generatedMesh == null)
            {
                return;
            }

            Mesh mesh = marchingCubesMesher.GenerateMesh(
                voxelDataManager.SdfBuffer,
                voxelDataManager.GridResolution,
                voxelDataManager.VoxelSize,
                new Vector3(-(voxelDataManager.GridResolution - 1) * voxelDataManager.VoxelSize * 0.5f,
                            -(voxelDataManager.GridResolution - 1) * voxelDataManager.VoxelSize * 0.5f,
                            -(voxelDataManager.GridResolution - 1) * voxelDataManager.VoxelSize * 0.5f),
                isoLevel,
                runtimeData);

            if (mesh == null)
            {
                return;
            }

            DestroyImmediate(generatedMesh);
            generatedMesh = mesh;
            meshFilter.sharedMesh = generatedMesh;
            meshCollider.sharedMesh = generatedMesh;
            shadingDirty = false;
        }

        private void RebuildComposeCache(DynamicPlanet other, float blendFactor)
        {
            composeCommandCache.Clear();

            for (int i = 0; i < persistentDamageStamps.Count; i++)
            {
                composeCommandCache.Add(persistentDamageStamps[i].ToComposeCommand());
            }

            if (other != null && blendFactor > 0f)
            {
                Vector3 otherLocalCenter = transform.InverseTransformPoint(other.transform.position);
                float otherRadius = other.runtimeDataReady ? other.runtimeData.radius : other.fallbackRadius;
                composeCommandCache.Add(SdfComposeCommand.CreateBlendSphere(
                    otherLocalCenter,
                    Mathf.Max(1f, otherRadius),
                    Mathf.Max(0.001f, blendFactor)));
            }
            else if (other != null)
            {
                Vector3 otherLocalCenter = transform.InverseTransformPoint(other.transform.position);
                float otherRadius = other.runtimeDataReady ? other.runtimeData.radius : other.fallbackRadius;
                composeCommandCache.Add(SdfComposeCommand.CreateBlendSphere(
                    otherLocalCenter,
                    Mathf.Max(1f, otherRadius),
                    defaultBlendStrength));
            }
        }

        private int ResolveLodResolution()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return nearResolution;
            }

            float distance = Vector3.Distance(cam.transform.position, transform.position);
            if (distance <= nearDistance)
            {
                return nearResolution;
            }

            if (distance <= farDistance)
            {
                return midResolution;
            }

            return farResolution;
        }

        private void ResolveRuntimeData()
        {
            if (runtimeDataReady)
            {
                return;
            }

            if (useFactoryGeneration)
            {
                try
                {
                    if (preferSelectedTemplate && selectedTemplate != null)
                    {
                        runtimeData = CelestialBodyFactory.GenerateFromTemplate(seed, selectedTemplate, randomizeSelectedTemplateRanges);
                    }
                    else if (templatePool != null && templatePool.Length > 0)
                    {
                        runtimeData = CelestialBodyFactory.Generate(seed, bodyClass, templatePool);
                    }
                    else
                    {
                        runtimeData = CreateFallbackRuntimeData();
                    }

                    runtimeData = NormalizeForDynamicPlanet(runtimeData);
                    runtimeDataReady = true;
                    hasLoggedFactoryError = false;
                    return;
                }
                catch (Exception ex)
                {
                    if (!hasLoggedFactoryError)
                    {
                        hasLoggedFactoryError = true;
                        Debug.LogWarning($"[DynamicPlanet] Template/factory read failed ({ex.Message}). Falling back to local runtime defaults.", this);
                    }
                }
            }

            runtimeData = NormalizeForDynamicPlanet(CreateFallbackRuntimeData());
            runtimeDataReady = true;
        }

        private RuntimeBodyData NormalizeForDynamicPlanet(RuntimeBodyData data)
        {
            if ((data.generationMode & GenerationMode.SolidSdf) == 0)
            {
                data.generationMode |= GenerationMode.SolidSdf;
                if (!hasLoggedModeOverride)
                {
                    hasLoggedModeOverride = true;
                    Debug.LogWarning("[DynamicPlanet] Selected template is not SolidSdf. DynamicPlanet forces SolidSdf so marching cubes can run.", this);
                }
            }

            bool zeroNoise =
                Mathf.Approximately(data.noiseLayerConfig.continent.amplitude, 0f) &&
                Mathf.Approximately(data.noiseLayerConfig.mountain.amplitude, 0f) &&
                Mathf.Approximately(data.noiseLayerConfig.detail.amplitude, 0f);

            if (zeroNoise)
            {
                RuntimeBodyData fallback = CreateFallbackRuntimeData();
                data.noiseLayerConfig = fallback.noiseLayerConfig;
                data.planetShapeConfig = fallback.planetShapeConfig;
                data.moonShapeConfig = fallback.moonShapeConfig;
            }

            if (data.baseShapeConfig.verticalSquash <= 0f)
            {
                data.baseShapeConfig.verticalSquash = 1f;
            }

            float maxOffset = Mathf.Max(2f, data.radius * 0.08f);
            data.baseShapeConfig.commonOffset = Vector3.ClampMagnitude(data.baseShapeConfig.commonOffset, maxOffset);

            if (data.shapeModel == ShapeModel.Moon)
            {
                if (data.moonShapeConfig.craterRadiusRange.x <= 0f)
                {
                    data.moonShapeConfig.craterRadiusRange.x = 0.005f;
                }
                if (data.moonShapeConfig.craterRadiusRange.y <= data.moonShapeConfig.craterRadiusRange.x)
                {
                    data.moonShapeConfig.craterRadiusRange.y = data.moonShapeConfig.craterRadiusRange.x + 0.005f;
                }
                if (data.moonShapeConfig.craterDepth <= 0f)
                {
                    data.moonShapeConfig.craterDepth = 0.001f;
                }
                if (data.moonShapeConfig.craterRimSharpness <= 0f)
                {
                    data.moonShapeConfig.craterRimSharpness = 0.01f;
                }
                if (data.moonShapeConfig.craterNoiseScale <= 0f)
                {
                    data.moonShapeConfig.craterNoiseScale = 0.001f;
                }
            }

            return data;
        }

        private void MarkRuntimeDataDirty(bool markShape, bool markShading, bool markCompose)
        {
            runtimeDataReady = false;
            activeResolution = -1;
            nextRetryTime = 0f;
            hasLoggedPipelineWarning = false;
            hasLoggedModeOverride = false;
            lastTemplateSourceHash = ComputeTemplateSourceHash();
            shapeDirty |= markShape;
            shadingDirty |= markShading;
            composeDirty |= markCompose;
        }

        private bool HasTemplateSourceChanged()
        {
            int current = ComputeTemplateSourceHash();
            if (current == lastTemplateSourceHash)
            {
                return false;
            }

            lastTemplateSourceHash = current;
            return true;
        }

        private int ComputeTemplateSourceHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + useFactoryGeneration.GetHashCode();
                hash = hash * 31 + seed;
                hash = hash * 31 + (int)bodyClass;
                hash = hash * 31 + preferSelectedTemplate.GetHashCode();
                hash = hash * 31 + randomizeSelectedTemplateRanges.GetHashCode();

                if (selectedTemplate != null)
                {
                    hash = hash * 31 + selectedTemplate.GetInstanceID();
                    hash = hash * 31 + selectedTemplate.ChangeVersion;
                }

                if (templatePool != null)
                {
                    hash = hash * 31 + templatePool.Length;
                    for (int i = 0; i < templatePool.Length; i++)
                    {
                        CelestialBodyTemplate t = templatePool[i];
                        if (t == null)
                        {
                            hash = hash * 31 + 1;
                            continue;
                        }

                        hash = hash * 31 + t.GetInstanceID();
                        hash = hash * 31 + t.ChangeVersion;
                    }
                }

                return hash;
            }
        }

        private void ApplyRuntimePhysicsBinding()
        {
            if (!runtimeDataReady)
            {
                return;
            }

            ProceduralBodyPhysicsBinder.Apply(gameObject, runtimeData);
            GravityWell well = GetComponent<GravityWell>();
            if (well != null)
            {
                well.ApplyProceduralBody(runtimeData.mass, runtimeData.radius);
            }
        }

        private RuntimeBodyData CreateFallbackRuntimeData()
        {
            return new RuntimeBodyData
            {
                bodyClass = bodyClass,
                generationMode = GenerationMode.SolidSdf,
                shapeModel = bodyClass == BodyClass.Moon ? ShapeModel.Moon : ShapeModel.Planet,
                shadingModel = bodyClass == BodyClass.Moon || bodyClass == BodyClass.AsteroidCluster ? ShadingModel.MoonBiomes : ShadingModel.PlanetBands,
                mass = fallbackMass,
                radius = fallbackRadius,
                density = 1f,
                rotationSpeed = 0f,
                temperature = 280f,
                albedo = 0.4f,
                anomalyChance = 0f,
                hasSurface = true,
                hasAtmosphere = false,
                hasEventHorizon = false,
                supportsLanding = true,
                radiationHazard = false,
                corePressureSupport = 10f,
                fractureThreshold = 18f,
                collapseThreshold = 24f,
                novaThreshold = 1f,
                structuralDamping = 4f,
                enableMeshNodeDeformation = false,
                meshTidalStartThreshold = 0.02f,
                meshTidalMaxThreshold = 2f,
                meshAxialStretchAtFull = 1.6f,
                meshRadialSqueezeAtFull = 0.55f,
                noiseLayerConfig = new NoiseLayerConfig
                {
                    continent = CreateNoiseLayer(0.01f, 4, 20f, 0.5f, 2f, new Vector3(17f, 31f, 53f)),
                    mountain = CreateNoiseLayer(0.03f, 4, 9f, 0.55f, 2f, new Vector3(113f, 79f, 41f)),
                    detail = CreateNoiseLayer(0.08f, 3, 2f, 0.5f, 2f, new Vector3(199f, 157f, 89f))
                },
                baseShapeConfig = new BaseShapeConfig
                {
                    commonOffset = Vector3.zero,
                    radiusBias = 0f,
                    verticalSquash = 1f
                },
                planetShapeConfig = new PlanetShapeConfig
                {
                    continent = CreateNoiseLayer(0.01f, 4, 20f, 0.5f, 2f, new Vector3(17f, 31f, 53f)),
                    mountain = CreateNoiseLayer(0.03f, 4, 9f, 0.55f, 2f, new Vector3(113f, 79f, 41f)),
                    detail = CreateNoiseLayer(0.08f, 3, 2f, 0.5f, 2f, new Vector3(199f, 157f, 89f)),
                    mask = CreateNoiseLayer(0.015f, 3, 1f, 0.5f, 2f, new Vector3(67f, 23f, 149f)),
                    oceanDepthMultiplier = 2f,
                    oceanFloorDepth = 1f,
                    oceanFloorSmoothing = 0.5f,
                    mountainBlend = 1f
                },
                moonShapeConfig = new MoonShapeConfig
                {
                    shape = CreateNoiseLayer(0.02f, 4, 6f, 0.5f, 2f, new Vector3(13f, 31f, 59f)),
                    ridgeA = CreateNoiseLayer(0.035f, 4, 4f, 0.45f, 2.1f, new Vector3(71f, 97f, 127f)),
                    ridgeB = CreateNoiseLayer(0.07f, 3, 2.5f, 0.5f, 2.2f, new Vector3(149f, 181f, 211f)),
                    craterCount = 24,
                    craterRadiusRange = new Vector2(4f, 18f),
                    craterDepth = 2f,
                    craterRimSharpness = 2f,
                    craterNoiseScale = 0.03f
                },
                planetShadingConfig = new PlanetShadingConfig
                {
                    largeNoise = CreateNoiseLayer(0.008f, 3, 1f, 0.5f, 2f, new Vector3(11f, 29f, 47f)),
                    smallNoise = CreateNoiseLayer(0.03f, 3, 1f, 0.5f, 2f, new Vector3(89f, 107f, 131f)),
                    detailNoise = CreateNoiseLayer(0.07f, 2, 1f, 0.5f, 2f, new Vector3(151f, 173f, 197f)),
                    detailWarpNoise = CreateNoiseLayer(0.02f, 2, 1f, 0.5f, 2f, new Vector3(211f, 223f, 239f))
                },
                moonShadingConfig = new MoonShadingConfig
                {
                    biomePointCount = 18,
                    biomeRadiusRange = new Vector2(0.08f, 0.22f),
                    biomeWarpNoise = CreateNoiseLayer(0.015f, 2, 1f, 0.5f, 2f, new Vector3(17f, 37f, 73f)),
                    detailNoise = CreateNoiseLayer(0.08f, 2, 1f, 0.5f, 2f, new Vector3(101f, 139f, 167f)),
                    detailWarpNoise = CreateNoiseLayer(0.03f, 2, 1f, 0.5f, 2f, new Vector3(191f, 223f, 251f)),
                    candidatePoolSize = 0.25f,
                    desiredEjectaRays = 2,
                    ejectaRaysScale = 10f
                },
                moonSurfaceConfig = new MoonSurfaceConfig
                {
                    mainTextureScale = 0.012f,
                    flatSurfaceScale = 0.03f,
                    steepSurfaceScale = 0.045f,
                    textureBlendStrength = 0.28f,
                    ejectaBrightness = 0.14f,
                    steepDarkening = 0.16f
                }
            };
        }

        private static NoiseLayer CreateNoiseLayer(float scale, int octaves, float amplitude, float persistence, float lacunarity, Vector3 offset)
        {
            return new NoiseLayer
            {
                scale = scale,
                octaves = octaves,
                amplitude = amplitude,
                persistence = persistence,
                lacunarity = lacunarity,
                offset = offset
            };
        }

        private void OnDestroy()
        {
            voxelDataManager?.ReleaseResources();
            marchingCubesMesher?.ReleaseResources();

            if (generatedMesh != null)
            {
                DestroyImmediate(generatedMesh);
                generatedMesh = null;
            }

            if (autoMaterial != null)
            {
                DestroyImmediate(autoMaterial);
                autoMaterial = null;
            }
        }

        private void OnDisable()
        {
            voxelDataManager?.ReleaseResources();
            marchingCubesMesher?.ReleaseResources();
        }

        private void OnValidate()
        {
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshCollider == null) meshCollider = GetComponent<MeshCollider>();
            if (voxelDataManager == null) voxelDataManager = GetComponent<VoxelDataManager>();
            if (marchingCubesMesher == null) marchingCubesMesher = GetComponent<MarchingCubesMesher>();

            MarkRuntimeDataDirty(markShape: true, markShading: true, markCompose: true);
            EnsureVisibleMaterial();

            if (!Application.isPlaying && generateInEditMode && isActiveAndEnabled)
            {
                RegenerateMesh();
            }
        }

        private void ScheduleRetry(string reason)
        {
            nextRetryTime = Time.realtimeSinceStartup + Mathf.Max(0.1f, retryIntervalSeconds);
            if (hasLoggedPipelineWarning)
            {
                return;
            }

            hasLoggedPipelineWarning = true;
            Debug.LogWarning($"[DynamicPlanet] Mesh generation paused: {reason}", this);
        }

        private void EnsureVisibleMaterial()
        {
            if (meshRenderer == null)
            {
                return;
            }

            Material current = meshRenderer.sharedMaterial;
            bool isLegacyUnlit = current != null && current.shader != null && current.shader.name == "Vortex/VertexColorUnlit";
            bool isAuto = current != null && current.name.StartsWith("DynamicPlanet_AutoMaterial");
            bool isUnsupported = current != null && current.shader != null &&
                                 current.shader.name != "Standard" &&
                                 current.shader.name != "Universal Render Pipeline/Lit" &&
                                 current.shader.name != "HDRP/Lit" &&
                                 current.shader.name != "Vortex/DynamicMoonSurface";

            Shader shader = FindBestRenderShader();
            bool shaderChanged = current != null && isAuto && shader != null && current.shader != shader;

            bool shouldCreateAutoMaterial = current == null || isAuto || isLegacyUnlit || isUnsupported || forceHdrpLitMaterial || shaderChanged;
            if (!shouldCreateAutoMaterial)
            {
                return;
            }

            if (shader == null)
            {
                if (!hasLoggedMissingRenderShader)
                {
                    hasLoggedMissingRenderShader = true;
                    Debug.LogWarning("[DynamicPlanet] No compatible render shader found (HDRP/URP/Built-in).", this);
                }

                return;
            }

            if (autoMaterial != null)
            {
                DestroyImmediate(autoMaterial);
            }

            autoMaterial = new Material(shader) { name = "DynamicPlanet_AutoMaterial" };
            if (autoMaterial.HasProperty("_SurfaceType")) autoMaterial.SetFloat("_SurfaceType", 0f);
            if (autoMaterial.HasProperty("_AlphaCutoffEnable")) autoMaterial.SetFloat("_AlphaCutoffEnable", 0f);
            if (autoMaterial.HasProperty("_BlendMode")) autoMaterial.SetFloat("_BlendMode", 0f);
            if (autoMaterial.HasProperty("_ZWrite")) autoMaterial.SetFloat("_ZWrite", 1f);
            if (autoMaterial.HasProperty("_DoubleSidedEnable")) autoMaterial.SetFloat("_DoubleSidedEnable", 1f);
            if (autoMaterial.HasProperty("_DoubleSidedNormalMode")) autoMaterial.SetFloat("_DoubleSidedNormalMode", 0f);
            if (autoMaterial.HasProperty("_CullMode")) autoMaterial.SetFloat("_CullMode", 0f);
            if (autoMaterial.HasProperty("_BaseColor")) autoMaterial.SetColor("_BaseColor", Color.white);
            if (autoMaterial.HasProperty("_Color")) autoMaterial.SetColor("_Color", Color.white);
            meshRenderer.sharedMaterial = autoMaterial;
        }

        private void ApplyRuntimeMaterialProperties()
        {
            if (meshRenderer == null || meshRenderer.sharedMaterial == null || !runtimeDataReady)
            {
                return;
            }

            Material material = meshRenderer.sharedMaterial;
            if (runtimeData.bodyClass == BodyClass.Moon)
            {
                MoonSurfaceConfig surface = runtimeData.moonSurfaceConfig;
                if (material.HasProperty("_MoonNoiseTex")) material.SetTexture("_MoonNoiseTex", surface.mainTexture);
                if (material.HasProperty("_CraterRayTex")) material.SetTexture("_CraterRayTex", surface.craterRayTexture);
                if (material.HasProperty("_FlatSurfaceTex")) material.SetTexture("_FlatSurfaceTex", surface.flatSurfaceTexture);
                if (material.HasProperty("_SteepSurfaceTex")) material.SetTexture("_SteepSurfaceTex", surface.steepSurfaceTexture);
                if (material.HasProperty("_MoonNoiseScale")) material.SetFloat("_MoonNoiseScale", Mathf.Max(0.0001f, surface.mainTextureScale));
                if (material.HasProperty("_FlatSurfaceScale")) material.SetFloat("_FlatSurfaceScale", Mathf.Max(0.0001f, surface.flatSurfaceScale));
                if (material.HasProperty("_SteepSurfaceScale")) material.SetFloat("_SteepSurfaceScale", Mathf.Max(0.0001f, surface.steepSurfaceScale));
                if (material.HasProperty("_TextureBlendStrength")) material.SetFloat("_TextureBlendStrength", Mathf.Clamp01(surface.textureBlendStrength));
                if (material.HasProperty("_EjectaBrightness")) material.SetFloat("_EjectaBrightness", Mathf.Clamp(surface.ejectaBrightness, 0f, 2f));
                if (material.HasProperty("_SteepDarkening")) material.SetFloat("_SteepDarkening", Mathf.Clamp01(surface.steepDarkening));
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.2f);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.2f);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.02f);
            }
        }

        private static float EstimateRequiredPaddingMultiplier(RuntimeBodyData data)
        {
            float maxDisplacement = Mathf.Abs(data.baseShapeConfig.radiusBias) * data.radius;
            maxDisplacement += data.baseShapeConfig.commonOffset.magnitude;

            if (data.shapeModel == ShapeModel.Moon)
            {
                maxDisplacement += data.moonShapeConfig.shape.amplitude;
                maxDisplacement += data.moonShapeConfig.ridgeA.amplitude;
                maxDisplacement += data.moonShapeConfig.ridgeB.amplitude;
                maxDisplacement += data.moonShapeConfig.craterDepth * data.radius * 1.5f;
            }
            else
            {
                maxDisplacement += Mathf.Abs(data.planetShapeConfig.continent.amplitude);
                maxDisplacement += Mathf.Abs(data.planetShapeConfig.mountain.amplitude);
                maxDisplacement += Mathf.Abs(data.planetShapeConfig.detail.amplitude);
                maxDisplacement += Mathf.Abs(data.planetShapeConfig.oceanFloorDepth);
            }

            return 1f + Mathf.Clamp(maxDisplacement / Mathf.Max(1f, data.radius), 0.12f, 0.55f);
        }

        private Shader FindBestRenderShader()
        {
            string[] shaderNames =
            {
                runtimeDataReady && runtimeData.bodyClass == BodyClass.Moon ? "Vortex/DynamicMoonSurface" : null,
                "HDRP/Lit",
                "Universal Render Pipeline/Lit",
                "Standard",
                "Unlit/Color"
            };

            for (int i = 0; i < shaderNames.Length; i++)
            {
                if (string.IsNullOrEmpty(shaderNames[i]))
                {
                    continue;
                }

                Shader s = Shader.Find(shaderNames[i]);
                if (s != null)
                {
                    return s;
                }
            }

            return null;
        }
    }
}
