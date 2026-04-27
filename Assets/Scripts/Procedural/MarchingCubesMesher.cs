using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Vortex.Procedural
{
    public sealed class MarchingCubesMesher : MonoBehaviour
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct GpuVertex
        {
            public Vector3 position;
            public Vector3 normal;
            public Vector2Int id;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GpuTriangle
        {
            public GpuVertex vertexA;
            public GpuVertex vertexB;
            public GpuVertex vertexC;
        }

        [Header("Compute")]
        [SerializeField] private ComputeShader marchingCubesShader;
        [SerializeField] private bool autoAssignShaderInEditor = true;
        [SerializeField, Min(1)] private int maxTriangleCount = 800000;

        [SerializeField] private bool forceDoubleSidedIndices = true;
        [SerializeField] private bool weldVertices = true;

        private const string KernelName = "CSMain";
        private const int ThreadsPerAxis = 4;

        private int kernelIndex = -1;
        private ComputeBuffer triangleBuffer;
        private ComputeBuffer counterBuffer;
        private bool hasLoggedMissingShader;
        private bool hasLoggedMissingKernel;
        private bool kernelRuntimeInvalid;

        #if UNITY_EDITOR
        private bool playModeHookRegistered;
        #endif

        private void OnEnable()
        {
            #if UNITY_EDITOR
            RegisterPlayModeHook();
            #endif
        }

        public Mesh GenerateMesh(
            ComputeBuffer sdfBuffer,
            int gridResolution,
            float voxelSize,
            Vector3 gridOrigin,
            float isoLevel)
        {
            EnsureInitialized();
            if (marchingCubesShader == null)
            {
                LogMissingShaderOnce();
                return null;
            }

            if (!marchingCubesShader.HasKernel(KernelName))
            {
                kernelIndex = -1;
                kernelRuntimeInvalid = true;
                LogMissingKernelOnce();
                return null;
            }

            if (kernelIndex < 0 || kernelRuntimeInvalid)
            {
                kernelIndex = marchingCubesShader.FindKernel(KernelName);
                kernelRuntimeInvalid = false;
            }

            if (sdfBuffer == null)
            {
                return null;
            }

            if (!DispatchAndReadCount(sdfBuffer, gridResolution, voxelSize, gridOrigin, isoLevel, out int triangleCount))
            {
                return null;
            }

            int growTries = 0;
            while (triangleCount >= maxTriangleCount && growTries < 4)
            {
                int previousCapacity = maxTriangleCount;
                int grownCapacity = Mathf.Min(maxTriangleCount * 2, 8000000);
                if (grownCapacity <= previousCapacity)
                {
                    break;
                }

                maxTriangleCount = grownCapacity;
                EnsureInitialized();

                if (!DispatchAndReadCount(sdfBuffer, gridResolution, voxelSize, gridOrigin, isoLevel, out triangleCount))
                {
                    return null;
                }

                growTries++;
                Debug.LogWarning($"[MarchingCubesMesher] Triangle buffer auto-grown {previousCapacity} -> {maxTriangleCount}.", this);
            }

            if (triangleCount >= maxTriangleCount)
            {
                Debug.LogWarning($"[MarchingCubesMesher] Triangle buffer reached hard cap ({maxTriangleCount}). Mesh may appear torn.", this);
            }

            if (triangleCount <= 0)
            {
                return new Mesh { name = "MarchingCubesMesh_Empty" };
            }

            GpuTriangle[] triangles = new GpuTriangle[triangleCount];
            triangleBuffer.GetData(triangles, 0, 0, triangleCount);

            BuildMeshStreams(
                triangles,
                triangleCount,
                out Vector3[] vertices,
                out Vector3[] normals,
                out int[] indices);

            int vertexCount = vertices.Length;

            Mesh mesh = new Mesh
            {
                name = "MarchingCubesMesh"
            };

            if (vertexCount > 65535)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.SetTriangles(indices, 0, true);
            mesh.RecalculateBounds();
            return mesh;
        }

        private void BuildMeshStreams(
            GpuTriangle[] triangles,
            int triangleCount,
            out Vector3[] vertices,
            out Vector3[] normals,
            out int[] indices)
        {
            if (!weldVertices)
            {
                int vertexCount = triangleCount * 3;
                vertices = new Vector3[vertexCount];
                normals = new Vector3[vertexCount];
                indices = forceDoubleSidedIndices ? new int[vertexCount * 2] : new int[vertexCount];

                int v = 0;
                for (int i = 0; i < triangleCount; i++)
                {
                    GpuTriangle tri = triangles[i];
                    vertices[v] = tri.vertexA.position; normals[v] = tri.vertexA.normal; if (!forceDoubleSidedIndices) indices[v] = v; v++;
                    vertices[v] = tri.vertexB.position; normals[v] = tri.vertexB.normal; if (!forceDoubleSidedIndices) indices[v] = v; v++;
                    vertices[v] = tri.vertexC.position; normals[v] = tri.vertexC.normal; if (!forceDoubleSidedIndices) indices[v] = v; v++;
                }

                if (forceDoubleSidedIndices)
                {
                    int t = 0;
                    for (int i = 0; i < vertexCount; i += 3)
                    {
                        int a = i; int b = i + 1; int c = i + 2;
                        indices[t++] = a; indices[t++] = b; indices[t++] = c;
                        indices[t++] = a; indices[t++] = c; indices[t++] = b;
                    }
                }

                return;
            }

            Dictionary<Vector2Int, int> map = new Dictionary<Vector2Int, int>(triangleCount * 2);
            List<Vector3> vtx = new List<Vector3>(triangleCount * 2);
            List<Vector3> nrm = new List<Vector3>(triangleCount * 2);
            List<int> triIdx = new List<int>(triangleCount * 3);

            AddTriangle(triangles, triangleCount, map, vtx, nrm, triIdx);

            vertices = vtx.ToArray();
            normals = nrm.ToArray();

            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = normals[i].sqrMagnitude > 0f ? normals[i].normalized : Vector3.up;
            }

            if (forceDoubleSidedIndices)
            {
                int triCount = triIdx.Count / 3;
                indices = new int[triIdx.Count * 2];
                int t = 0;
                for (int i = 0; i < triCount; i++)
                {
                    int a = triIdx[i * 3 + 0];
                    int b = triIdx[i * 3 + 1];
                    int c = triIdx[i * 3 + 2];
                    indices[t++] = a; indices[t++] = b; indices[t++] = c;
                    indices[t++] = a; indices[t++] = c; indices[t++] = b;
                }
            }
            else
            {
                indices = triIdx.ToArray();
            }
        }

        private static void AddTriangle(
            GpuTriangle[] triangles,
            int triangleCount,
            Dictionary<Vector2Int, int> map,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<int> indices)
        {
            for (int i = 0; i < triangleCount; i++)
            {
                GpuTriangle tri = triangles[i];
                int a = GetOrCreateVertex(tri.vertexA, map, vertices, normals);
                int b = GetOrCreateVertex(tri.vertexB, map, vertices, normals);
                int c = GetOrCreateVertex(tri.vertexC, map, vertices, normals);

                if (a == b || b == c || a == c)
                {
                    continue;
                }

                indices.Add(a);
                indices.Add(b);
                indices.Add(c);
            }
        }

        private static int GetOrCreateVertex(
            GpuVertex gpuVertex,
            Dictionary<Vector2Int, int> map,
            List<Vector3> vertices,
            List<Vector3> normals)
        {
            Vector2Int key = gpuVertex.id;

            if (map.TryGetValue(key, out int index))
            {
                normals[index] += gpuVertex.normal;
                return index;
            }

            int created = vertices.Count;
            map.Add(key, created);
            vertices.Add(gpuVertex.position);
            normals.Add(gpuVertex.normal);
            return created;
        }

        private bool DispatchAndReadCount(
            ComputeBuffer sdfBuffer,
            int gridResolution,
            float voxelSize,
            Vector3 gridOrigin,
            float isoLevel,
            out int triangleCount)
        {
            triangleCount = 0;

            triangleBuffer.SetCounterValue(0);

            marchingCubesShader.SetInt("_GridResolution", gridResolution);
            marchingCubesShader.SetFloat("_VoxelSize", voxelSize);
            marchingCubesShader.SetVector("_GridOrigin", new Vector4(gridOrigin.x, gridOrigin.y, gridOrigin.z, 0f));
            marchingCubesShader.SetFloat("_IsoLevel", isoLevel);
            marchingCubesShader.SetBuffer(kernelIndex, "_SdfInput", sdfBuffer);
            marchingCubesShader.SetBuffer(kernelIndex, "_Triangles", triangleBuffer);

            int cellResolution = Mathf.Max(1, gridResolution - 1);
            int groups = Mathf.CeilToInt(cellResolution / (float)ThreadsPerAxis);
            try
            {
                marchingCubesShader.Dispatch(kernelIndex, groups, groups, groups);
            }
            catch (Exception ex)
            {
                kernelRuntimeInvalid = true;
                Debug.LogWarning($"[MarchingCubesMesher] Dispatch failed, kernel marked invalid: {ex.Message}", this);
                return false;
            }

            ComputeBuffer.CopyCount(triangleBuffer, counterBuffer, 0);
            int[] triCounter = { 0 };
            counterBuffer.GetData(triCounter);
            triangleCount = triCounter[0];
            return true;
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

        public void ReleaseResources()
        {
            ReleaseBuffers();
        }

        private void EnsureInitialized()
        {
            TryResolveShader();

            if (marchingCubesShader == null)
            {
                return;
            }

            if (kernelIndex < 0)
            {
                if (!marchingCubesShader.HasKernel(KernelName))
                {
                    LogMissingKernelOnce();
                    return;
                }

                kernelIndex = marchingCubesShader.FindKernel(KernelName);
                kernelRuntimeInvalid = false;
            }

            if (triangleBuffer == null || triangleBuffer.count != maxTriangleCount)
            {
                ReleaseBuffers();
                int stride = Marshal.SizeOf<GpuTriangle>();
                triangleBuffer = new ComputeBuffer(maxTriangleCount, stride, ComputeBufferType.Append);
                counterBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
            }
        }

        private void TryResolveShader()
        {
            if (marchingCubesShader != null)
            {
                return;
            }

            #if UNITY_EDITOR
            if (autoAssignShaderInEditor)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("MarchingCubesMesher t:ComputeShader");
                if (guids != null && guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    marchingCubesShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(path);
                    kernelRuntimeInvalid = false;
                }
            }
            #endif
        }

        private void LogMissingShaderOnce()
        {
            if (hasLoggedMissingShader)
            {
                return;
            }

            hasLoggedMissingShader = true;
            Debug.LogWarning("[MarchingCubesMesher] MarchingCubesMesher.compute is not assigned. Assign it on the component or keep auto-assign enabled.", this);
        }

        private void LogMissingKernelOnce()
        {
            if (hasLoggedMissingKernel)
            {
                return;
            }

            hasLoggedMissingKernel = true;
            Debug.LogWarning($"[MarchingCubesMesher] Kernel '{KernelName}' not found in assigned compute shader. Check shader compile errors.", this);
        }

        private void ReleaseBuffers()
        {
            if (triangleBuffer != null)
            {
                triangleBuffer.Release();
                triangleBuffer.Dispose();
                triangleBuffer = null;
            }

            if (counterBuffer != null)
            {
                counterBuffer.Release();
                counterBuffer.Dispose();
                counterBuffer = null;
            }

            kernelIndex = -1;
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
    }
}
