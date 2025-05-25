using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

/// <summary>
/// CPU版本是先通过计算着色器计算生成网格后，再生成的网格像常规一样渲染和设置物理碰撞。
/// </summary>
[RequireComponent(typeof(MeshFilter))]
public class MarchingCubesCPU : MonoBehaviour
{
    struct Vertex
    {
        public const int Size = sizeof(uint) * 4 + sizeof(float) * 4 + sizeof(float) * 4;

        public uint2 id;
        public float3 position;
        public float3 normal;
    }

    struct Triangle
    {
        public const int Size = Vertex.Size * 3;

        public Vertex p0;
        public Vertex p1;
        public Vertex p2;

        public Vertex this[int i] => i switch {
            0 => p0,
            1 => p1,
            2 => p2,
            _ => throw new ArgumentOutOfRangeException(nameof(i), i, null)
        };
    }

    [SerializeField] ComputeShader computeShader;
    [SerializeField] MarchingCubesWorld world;
    [SerializeField] bool realTimeUpdate;
    [SerializeField] MeshCollider updateMeshCollider;

    readonly int[] worldResolutionCPU = new int[3];
    readonly int[] worldDimensionCPU = new int[3];
    ComputeBuffer triangleVerticesBuffer;
    ComputeBuffer triangleVerticesBufferCounter;
    NativeArray<Triangle> triangleVerticesBufferCPU;
    NativeArray<int> triangleVerticesBufferCounterCPU;
    readonly AsyncGPUReadbackRequest[] requests = new AsyncGPUReadbackRequest[2];
    int buildProcess = -1;
    Mesh mesh;
    readonly Dictionary<uint2, int> vertexMap = new Dictionary<uint2, int>();
    readonly List<Vector3> positions = new List<Vector3>();
    readonly List<Vector3> normals = new List<Vector3>();
    readonly List<int> triangles = new List<int>();

    void Start()
    {
        //计算着色器相关
        triangleVerticesBuffer = new ComputeBuffer(world.WorldVolume * 5, Triangle.Size, ComputeBufferType.Append);
        triangleVerticesBufferCounter = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        triangleVerticesBufferCPU = new NativeArray<Triangle>(triangleVerticesBuffer.count, Allocator.Persistent);
        triangleVerticesBufferCounterCPU = new NativeArray<int>(triangleVerticesBufferCounter.count, Allocator.Persistent);
        (worldResolutionCPU[0], worldResolutionCPU[1], worldResolutionCPU[2]) = (world.WorldResolution.x, world.WorldResolution.y, world.WorldResolution.z);
        (worldDimensionCPU[0], worldDimensionCPU[1], worldDimensionCPU[2]) = (world.WorldDimension.x, world.WorldDimension.y, world.WorldDimension.z);
        //自定义网格生成
        mesh = new Mesh();
        mesh.bounds = new Bounds(Vector3.zero, (float3)world.WorldDimension);
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;
    }

    void Update()
    {
        if (buildProcess == -1)
        {
            //计算三角面
            Profiler.BeginSample("计算三角面");
            triangleVerticesBuffer.SetCounterValue(0);
            computeShader.SetTexture(0, "_World", world.World);
            computeShader.SetInts("_WorldResolution", worldResolutionCPU);
            computeShader.SetInts("_WorldDimension", worldDimensionCPU);
            computeShader.SetFloat("_WorldVisibleDensity", world.WorldVisibleDensity);
            computeShader.SetBuffer(0, "_Triangles", triangleVerticesBuffer);
            computeShader.Dispatch(0, world.WorldDimension.x, world.WorldDimension.y, world.WorldDimension.z);
            Profiler.EndSample();
            //获取三角面
            Profiler.BeginSample("获取三角面数量");
            ComputeBuffer.CopyCount(triangleVerticesBuffer, triangleVerticesBufferCounter, 0);
            requests[0] = AsyncGPUReadback.RequestIntoNativeArray(ref triangleVerticesBufferCounterCPU, triangleVerticesBufferCounter, sizeof(int), 0);
            Profiler.EndSample();

            buildProcess = -2;
        }

        if (buildProcess == -2 && requests[0].done)
        {
            int trianglesCount = triangleVerticesBufferCounterCPU[0];
            if (trianglesCount != 0) //为什么会出现这种情况？
            {
                Profiler.BeginSample("获取三角面");
                requests[1] = AsyncGPUReadback.RequestIntoNativeArray(ref triangleVerticesBufferCPU, triangleVerticesBuffer, triangleVerticesBufferCounterCPU[0] * Triangle.Size, 0);
                Profiler.EndSample();
                buildProcess = -3;
            }
            else
            {
                buildProcess = -1;
            }
        }

        if (buildProcess == -3 && requests[1].done)
        {
            //生成顶点和三角面索引
            Profiler.BeginSample("生成顶点和三角面索引");
            vertexMap.Clear();
            triangles.Clear();
            positions.Clear();
            normals.Clear();
            int trianglesCount = triangleVerticesBufferCounterCPU[0];
            for (int i = 0; i < trianglesCount; i++)
            {
                Triangle triangle = triangleVerticesBufferCPU[i];
                for (int j = 0; j < 3; j++)
                {
                    Vertex vertex = triangle[j];
                    if (vertexMap.TryGetValue(vertex.id, out int index))
                    {
                        triangles.Add(index);
                    }
                    else
                    {
                        triangles.Add(positions.Count);
                        vertexMap.Add(vertex.id, positions.Count);
                        positions.Add(vertex.position);
                        normals.Add(vertex.normal);
                    }
                }
            }
            Profiler.EndSample();
            //应用到网格并生成法线
            Profiler.BeginSample("应用到网格并生成法线");
            mesh.Clear();
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            Profiler.EndSample();
            //更新网格碰撞
            if (updateMeshCollider)
                updateMeshCollider.sharedMesh = mesh;
            //通知再次更新网格
            if (realTimeUpdate)
                buildProcess = -1;
            else
                buildProcess = -4;
        }
    }
    void OnDestroy()
    {
        foreach (AsyncGPUReadbackRequest request in requests)
            request.WaitForCompletion();

        triangleVerticesBuffer.Dispose();
        triangleVerticesBufferCounter.Dispose();
        triangleVerticesBufferCPU.Dispose();
        triangleVerticesBufferCounterCPU.Dispose();
    }
}
