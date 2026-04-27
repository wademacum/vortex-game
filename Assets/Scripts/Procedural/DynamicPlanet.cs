using UnityEngine;
using Vortex.Physics;
using System;

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
        [SerializeField, Min(1f)] private float radiusPaddingMultiplier = 1.2f;

        [Header("Meshing")]
        [SerializeField] private float isoLevel = 0f;
        [SerializeField] private bool regenerateOnStart = true;
        [SerializeField] private bool runtimeLodUpdates = true;
        [SerializeField] private bool generateInEditMode = true;
        [SerializeField, Min(0.1f)] private float retryIntervalSeconds = 0.75f;

        [Header("Rendering")]
        [SerializeField] private bool forceHdrpLitMaterial = true;

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
                MarkRuntimeDataDirty();
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

            if (!runtimeDataReady)
            {
                return;
            }

            if (Time.realtimeSinceStartup < nextRetryTime)
            {
                return;
            }

            int desired = ResolveLodResolution();
            if (desired != activeResolution)
            {
                RegenerateMesh();
            }
        }

        [ContextMenu("Refresh Runtime Data")]
        public void RefreshRuntimeData()
        {
            MarkRuntimeDataDirty();
            RegenerateMesh();
        }

        [ContextMenu("Regenerate Mesh")]
        public void RegenerateMesh()
        {
            ResolveRuntimeData();
            if (!runtimeDataReady)
            {
                Debug.LogWarning("[DynamicPlanet] Runtime data unavailable. Skipping mesh regeneration.", this);
                return;
            }

            int resolution = ResolveLodResolution();
            float diameter = Mathf.Max(1f, runtimeData.radius * 2f * radiusPaddingMultiplier);
            float voxelSize = diameter / Mathf.Max(1, resolution - 1);
            Vector3 gridOrigin = new Vector3(-diameter * 0.5f, -diameter * 0.5f, -diameter * 0.5f);

            voxelDataManager.ConfigureGrid(resolution, voxelSize);
            if (!voxelDataManager.Generate(runtimeData, gridOrigin))
            {
                ScheduleRetry("VoxelDataGenerator missing or not initialized.");
                return;
            }

            Mesh mesh = marchingCubesMesher.GenerateMesh(
                voxelDataManager.SdfBuffer,
                resolution,
                voxelSize,
                gridOrigin,
                isoLevel);

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

            EnsureVisibleMaterial();

            ApplyRuntimePhysicsBinding();
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

            runtimeData = CreateFallbackRuntimeData();
            runtimeData = NormalizeForDynamicPlanet(runtimeData);
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
            }

            return data;
        }

        private void MarkRuntimeDataDirty()
        {
            runtimeDataReady = false;
            activeResolution = -1;
            nextRetryTime = 0f;
            hasLoggedPipelineWarning = false;
            hasLoggedModeOverride = false;
            lastTemplateSourceHash = ComputeTemplateSourceHash();
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
                    continent = new NoiseLayer
                    {
                        scale = 0.01f,
                        octaves = 4,
                        amplitude = 20f,
                        persistence = 0.5f,
                        lacunarity = 2f,
                        offset = new Vector3(17f, 31f, 53f)
                    },
                    mountain = new NoiseLayer
                    {
                        scale = 0.03f,
                        octaves = 4,
                        amplitude = 9f,
                        persistence = 0.55f,
                        lacunarity = 2f,
                        offset = new Vector3(113f, 79f, 41f)
                    },
                    detail = new NoiseLayer
                    {
                        scale = 0.08f,
                        octaves = 3,
                        amplitude = 2f,
                        persistence = 0.5f,
                        lacunarity = 2f,
                        offset = new Vector3(199f, 157f, 89f)
                    }
                }
            };
        }

        private void OnDestroy()
        {
            if (voxelDataManager != null)
            {
                voxelDataManager.ReleaseResources();
            }

            if (marchingCubesMesher != null)
            {
                marchingCubesMesher.ReleaseResources();
            }

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
            if (voxelDataManager != null)
            {
                voxelDataManager.ReleaseResources();
            }

            if (marchingCubesMesher != null)
            {
                marchingCubesMesher.ReleaseResources();
            }
        }

        private void OnValidate()
        {
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }

            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
            }

            if (meshCollider == null)
            {
                meshCollider = GetComponent<MeshCollider>();
            }

            if (voxelDataManager == null)
            {
                voxelDataManager = GetComponent<VoxelDataManager>();
            }

            if (marchingCubesMesher == null)
            {
                marchingCubesMesher = GetComponent<MarchingCubesMesher>();
            }

            MarkRuntimeDataDirty();

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
            bool isLegacyUnlit = current != null && current.shader != null &&
                                current.shader.name == "Vortex/VertexColorUnlit";
            bool isAuto = current != null && current.name.StartsWith("DynamicPlanet_AutoMaterial");
            bool isUnsupported = current != null && current.shader != null &&
                                 current.shader.name != "Standard" &&
                                 current.shader.name != "Universal Render Pipeline/Lit" &&
                                 current.shader.name != "HDRP/Lit";

            bool shouldCreateAutoMaterial = current == null || isAuto || isLegacyUnlit || isUnsupported;
            if (!shouldCreateAutoMaterial)
            {
                return;
            }

            Shader shader = FindBestRenderShader();
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
                autoMaterial = null;
            }

            autoMaterial = new Material(shader)
            {
                name = "DynamicPlanet_AutoMaterial"
            };

            if (autoMaterial.HasProperty("_SurfaceType")) autoMaterial.SetFloat("_SurfaceType", 0f);
            if (autoMaterial.HasProperty("_AlphaCutoffEnable")) autoMaterial.SetFloat("_AlphaCutoffEnable", 0f);
            if (autoMaterial.HasProperty("_BlendMode")) autoMaterial.SetFloat("_BlendMode", 0f);
            if (autoMaterial.HasProperty("_ZWrite")) autoMaterial.SetFloat("_ZWrite", 1f);
            if (autoMaterial.HasProperty("_DoubleSidedEnable")) autoMaterial.SetFloat("_DoubleSidedEnable", 1f);
            if (autoMaterial.HasProperty("_DoubleSidedNormalMode")) autoMaterial.SetFloat("_DoubleSidedNormalMode", 0f);
            if (autoMaterial.HasProperty("_CullMode")) autoMaterial.SetFloat("_CullMode", 0f);
            if (autoMaterial.HasProperty("_BaseColor")) autoMaterial.SetColor("_BaseColor", Color.white);
            if (autoMaterial.HasProperty("_EmissiveColor")) autoMaterial.SetColor("_EmissiveColor", Color.black);
            if (autoMaterial.HasProperty("_Color")) autoMaterial.SetColor("_Color", Color.white);

            meshRenderer.sharedMaterial = autoMaterial;
        }

        private static Shader FindBestRenderShader()
        {
            string[] shaderNames =
            {
                "HDRP/Lit",
                "Universal Render Pipeline/Lit",
                "Standard",
                "Unlit/Color"
            };

            for (int i = 0; i < shaderNames.Length; i++)
            {
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
