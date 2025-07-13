using System;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class Texture3DAlgorithm : MonoBehaviour
{
    public static Texture3D MeshToWorld(Mesh mesh, int precision)
    {
        //创建3D纹理
        int3 textureResolution = (int3)(precision * math.round(mesh.bounds.size) + 0.001f) + 2; //包一层外壳，以引导体素物体为封闭网格
        Texture3D world = new Texture3D(
            textureResolution.x, textureResolution.y, textureResolution.z,
            GraphicsFormat.R32_SFloat, TextureCreationFlags.None
        );
        world.filterMode = FilterMode.Bilinear;
        world.wrapMode = TextureWrapMode.Clamp;

        //计算参数
        MeshWorldComputer computer = new MeshWorldComputer();
        computer.triangles = new NativeArray<int>(mesh.triangles, Allocator.TempJob);
        computer.vertices = new NativeArray<float3>(mesh.vertices.Select(vector3 => (float3)vector3).ToArray(), Allocator.TempJob);
        computer.resolution = new int3(world.width, world.height, world.depth);
        computer.voxelSize = (float3)mesh.bounds.size / (computer.resolution - 2);
        computer.center = mesh.bounds.center;
        //计算结果（选3个方向各进行一次，从而获取更精确的结果）
        int pixelCount = world.width * world.height * world.depth;
        NativeArray<float> pixelResult0 = new NativeArray<float>(pixelCount, Allocator.TempJob);
        NativeArray<float> pixelResult1 = new NativeArray<float>(pixelCount, Allocator.TempJob);
        NativeArray<float> pixelResult2 = new NativeArray<float>(pixelCount, Allocator.TempJob);
        NativeArray<float> pixels = world.GetPixelData<float>(0);
        //计算
        computer.rayDirection = new float3(1, 0, 0);
        computer.result = pixelResult0;
        JobHandle handle0 = computer.Schedule(pixelCount, 64);
        computer.rayDirection = new float3(0, 1, 0);
        computer.result = pixelResult1;
        JobHandle handle1 = computer.Schedule(pixelCount, 64);
        computer.rayDirection = new float3(0, 0, 1);
        computer.result = pixelResult2;
        JobHandle handle2 = computer.Schedule(pixelCount, 64);
        JobHandle.CompleteAll(ref handle0, ref handle1, ref handle2);
        //获取结果（当至少两个方向都检测出该点在网格内时，才认为结果算）
        for (int i = 0; i < pixelCount; i++)
            pixels[i] = pixelResult0[i] + pixelResult1[i] + pixelResult2[i] >= 2 ? 1 : 0;

        computer.triangles.Dispose();
        computer.vertices.Dispose();
        pixelResult0.Dispose();
        pixelResult1.Dispose();
        pixelResult2.Dispose();

        return world;
    }

    /// <summary>
    /// 将 input 中最小的连通域分离到 output 中
    /// </summary>
    /// <param name="input"></param>
    /// <param name="threshold"></param>
    /// <param name="output"></param>
    public static int SeparatedConnectedComponent(Texture3D input, float threshold, Texture3D output)
    {
        int pixelCount = input.width * input.height * input.depth;
        NativeArray<byte> labels = new NativeArray<byte>(pixelCount, Allocator.TempJob);
        NativeArray<int> separatedPixelCount = new NativeArray<int>(1, Allocator.TempJob);

        SeparatedConnectedComponentComputer connectedComponentComputer = new SeparatedConnectedComponentComputer(
            input, threshold, labels, output, separatedPixelCount
        );
        connectedComponentComputer.Schedule().Complete();
        int result = separatedPixelCount[0];

        labels.Dispose();
        separatedPixelCount.Dispose();


        return result;
    }

    public static int ComputeVolume(Texture3D input, float visibleDensity)
    {
        NativeArray<int> result = new NativeArray<int>(1, Allocator.TempJob);

        VolumeComputer volumeComputer = new VolumeComputer {
            world = input.GetPixelData<float>(0),
            result = result,
            visibleDensity = visibleDensity
        };
        volumeComputer.Schedule().Complete();

        int volume = result[0];
        result.Dispose();

        return volume;
    }

    public static void DrawCapsuleArea(Texture3D world, float4x4 localToWorld, NativeArray<Capsule> capsules, float value)
    {
        DrawCapsuleAreaComputer capsuleAreaComputer;
        capsuleAreaComputer.world = world.GetPixelData<float>(0);
        capsuleAreaComputer.resolution = new int3(world.width, world.height, world.depth);
        capsuleAreaComputer.localToWorld = localToWorld;
        capsuleAreaComputer.capsules = capsules;
        capsuleAreaComputer.value = value;
        capsuleAreaComputer.Schedule(capsuleAreaComputer.world.Length, 64).Complete();
    }

    /// <summary>
    /// 清除目标区域的值
    /// </summary>
    /// <param name="world"></param>
    /// <param name="capsules"></param>
    public static void ClearCapsuleArea(MarchingCubesWorld world, NativeArray<Capsule> capsules)
    {
        DrawCapsuleArea(world.OriginalWorld, world.transform.localToWorldMatrix, capsules, -1);
    }

    /// <summary>
    /// https://jacklj.github.io/ccl/
    /// </summary>
    [BurstCompile]
    struct SeparatedConnectedComponentComputer : IJob
    {
        NativeArray<int> separatedPixelCount;

        public SeparatedConnectedComponentComputer(Texture3D input, float threshold, NativeArray<byte> labels,
            Texture3D output, NativeArray<int> separatedPixelCount
        )
        {
            this.input = input.GetPixelData<float>(0);
            this.output = output.GetPixelData<float>(0);
            resolution = new int3(input.width, input.height, input.depth);
            this.threshold = threshold;
            currentLabel = 0;
            this.labels = labels;
            this.separatedPixelCount = separatedPixelCount;
        }

        NativeArray<float> input;
        NativeArray<float> output;
        readonly int3 resolution;
        readonly float threshold;
        byte currentLabel;
        NativeArray<byte> labels;

        public unsafe void Execute()
        {
            NativeParallelHashMap<byte, UIntPtr> connectedComponents = new NativeParallelHashMap<byte, UIntPtr>(64, Allocator.Temp);
            NativeParallelHashMap<byte, int> labelCounts = new NativeParallelHashMap<byte, int>(64, Allocator.Temp);

            UnsafeList<byte>* GetConnectedComponent(byte label)
            {
                return (UnsafeList<byte>*)connectedComponents[label].ToPointer();
            }
            void SetConnectedComponent(byte label, UnsafeList<byte>* connectedComponent)
            {
                connectedComponents[label] = new UIntPtr(connectedComponent);
            }
            void CombineConnectedComponent(byte srcLabel, byte dstLabel)
            {
                UnsafeList<byte>* srcConnectedComponent = GetConnectedComponent(srcLabel);
                UnsafeList<byte>* dstConnectedComponent = GetConnectedComponent(dstLabel);

                foreach (byte label in *srcConnectedComponent)
                {
                    dstConnectedComponent->Add(label);
                    SetConnectedComponent(label, dstConnectedComponent);
                }
            }

            //计算像素初始标签，并生成标签连通信息
            for (int z = 0; z < resolution.z; z++)
            for (int y = 0; y < resolution.y; y++)
            for (int x = 0; x < resolution.x; x++)
            {
                int index = PixelToIndex(x, y, z);
                if (input[index] > threshold)
                {
                    int downIndex = PixelToIndex(x, math.max(y - 1, 0), z);
                    int leftIndex = PixelToIndex(math.max(x - 1, 0), y, z);
                    int backIndex = PixelToIndex(x, y, math.max(z - 1, 0));
                    byte downLabel = labels[downIndex];
                    byte leftLabel = labels[leftIndex];
                    byte backLabel = labels[backIndex];

                    if (downLabel == 0 && leftLabel == 0 && backLabel == 0)
                    {
                        //新建标签
                        currentLabel++;
                        NativeList<byte> connectedComponent = new NativeList<byte>(64, Allocator.Temp);
                        connectedComponent.Add(currentLabel);
                        SetConnectedComponent(currentLabel, connectedComponent.GetUnsafeList());
                        labels[index] = currentLabel;
                    }
                    else
                    {
                        byte downParentLabel = downLabel == 0 ? byte.MaxValue : GetConnectedComponent(downLabel)->Ptr[0];
                        byte leftParentLabel = leftLabel == 0 ? byte.MaxValue : GetConnectedComponent(leftLabel)->Ptr[0];
                        byte backParentLabel = backLabel == 0 ? byte.MaxValue : GetConnectedComponent(backLabel)->Ptr[0];
                        byte parentLabel = (byte)math.min(math.min((int)downParentLabel, leftParentLabel), backParentLabel);
                        labels[index] = parentLabel;

                        //合并连通分量
                        if (downParentLabel != byte.MaxValue && downParentLabel != parentLabel)
                            CombineConnectedComponent(downLabel, parentLabel);
                        if (leftParentLabel != byte.MaxValue && leftParentLabel != parentLabel)
                            CombineConnectedComponent(leftLabel, parentLabel);
                        if (backParentLabel != byte.MaxValue && backParentLabel != parentLabel)
                            CombineConnectedComponent(backLabel, parentLabel);
                    }
                }
            }

            //统一连通标签，并计算连通像素数量
            for (int z = 0; z < resolution.z; z++)
            for (int y = 0; y < resolution.y; y++)
            for (int x = 0; x < resolution.x; x++)
            {
                int index = PixelToIndex(x, y, z);
                byte label = labels[index];
                if (label == 0)
                    continue;

                label = GetConnectedComponent(label)->Ptr[0];
                labels[index] = label;
                if (labelCounts.TryGetValue(label, out int count))
                    labelCounts[label] = count + 1;
                else
                    labelCounts[label] = 0 + 1;
            }

            if (labelCounts.Count() <= 1)
                return;

            byte minLabel = 0;
            int minLabelCount = int.MaxValue;
            foreach (KeyValue<byte, int> pair in labelCounts)
            {
                if (pair.Value < minLabelCount)
                {
                    minLabel = pair.Key;
                    minLabelCount = pair.Value;
                }
            }

            if (minLabel != 0)
            {
                for (int x = 0; x < resolution.x; x++)
                for (int y = 0; y < resolution.y; y++)
                for (int z = 0; z < resolution.z; z++)
                {
                    int index = PixelToIndex(x, y, z);

                    if (labels[index] == minLabel)
                    {
                        output[index] = input[index];
                        input[index] = new float();
                    }
                }
                separatedPixelCount[0] = minLabelCount;
            }
            else
            {
                separatedPixelCount[0] = 0;
            }
        }

        int PixelToIndex(int x, int y, int z)
        {
            return x + y * resolution.x + z * resolution.y * resolution.x;
        }
    }

    [BurstCompile]
    struct DrawCapsuleAreaComputer : IJobParallelFor
    {
        public NativeArray<float> world;
        public int3 resolution;
        public float4x4 localToWorld;
        public float value;
        [ReadOnly] public NativeArray<Capsule> capsules;

        public void Execute(int index)
        {
            float3 positionWS = IndexToPositionWS(index);
            foreach (Capsule capsule in capsules)
            {
                if (capsule.IsContained(positionWS))
                {
                    world[index] = math.clamp(world[index] + value, 0, 1);
                    return;
                }
            }
        }

        float3 IndexToPositionWS(int index)
        {
            int3 pixel;
            int areaXY = resolution.x * resolution.y;
            pixel.z = index / areaXY;
            int surplus = index % areaXY;
            pixel.y = surplus / resolution.x;
            pixel.x = surplus % resolution.x;

            float3 positionOS = (float3)pixel - (resolution - 1) / 2;
            float3 positionWS = math.mul(localToWorld, new float4(positionOS, 1)).xyz;
            return positionWS;
        }
    }

    [BurstCompile]
    struct VolumeComputer : IJob
    {
        public NativeArray<float> world;
        public NativeArray<int> result;
        public float visibleDensity;

        public void Execute()
        {
            int volume = 0;
            int length = world.Length;
            for (int i = 0; i < length; i++)
            {
                if (world[i] >= visibleDensity)
                    volume++;
            }
            result[0] = volume;
        }
    }

    [BurstCompile]
    struct MeshWorldComputer : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> triangles;
        [ReadOnly] public NativeArray<float3> vertices;
        public float3 rayDirection;
        public NativeArray<float> result;
        public int3 resolution;
        public float3 voxelSize;
        public float3 center;

        public void Execute(int index)
        {
            float3 position = IndexToLocal(index);
            LineSegment ray = new LineSegment(position, position + rayDirection * 9999.0f);
            NativeParallelHashSet<int2> intersectantLines = new NativeParallelHashSet<int2>(8, Allocator.Temp);
            NativeParallelHashSet<float3> intersectantPoints = new NativeParallelHashSet<float3>(8, Allocator.Temp);
            int intersectionCount = 0;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int t0 = triangles[i + 0];
                int t1 = triangles[i + 1];
                int t2 = triangles[i + 2];
                Triangle triangle = new Triangle(vertices[t0], vertices[t1], vertices[t2]);

                //射线穿过三角面
                if (triangle.IntersectLineSegment(ray, out Vector3 intersection))
                {
                    //采样点就在面上，直接包含
                    if (math.lengthsq((float3)intersection - position) < float.Epsilon)
                    {
                        result[index] = 1;
                        return;
                    }

                    //射线穿过顶点
                    NativeArray<int> points = new NativeArray<int>(3, Allocator.Temp);
                    points[0] = t0;
                    points[1] = t1;
                    points[2] = t2;
                    foreach (int point in points)
                    {
                        //采样点就在顶点附近，直接包含
                        if (math.lengthsq(vertices[point] - position) < float.Epsilon)
                        {
                            result[index] = 1;
                            return;
                        }

                        //投影后的交点在顶点附近，避免重复点
                        if (math.lengthsq(vertices[point] - (float3)intersection) < float.Epsilon)
                        {
                            if (intersectantPoints.Add(vertices[point]) == false)
                            {
                                intersectionCount--;
                            }
                        }
                    }

                    //射线穿过线段时
                    NativeArray<int2> lines = new NativeArray<int2>(3, Allocator.Temp);
                    lines[0] = new int2(t0, t1);
                    lines[1] = new int2(t1, t2);
                    lines[2] = new int2(t2, t0);
                    foreach (int2 line in lines)
                    {
                        LineSegment lineSegment = new LineSegment(vertices[line.x], vertices[line.y]);
                        //采样点就在线段附近，直接包含
                        if (lineSegment.DistanceFromPoint(position, out Vector3 _) < float.Epsilon)
                        {
                            result[index] = 1;
                            return;
                        }

                        //投影后的交点在线段附近，避免重复点
                        if (lineSegment.DistanceFromPoint(intersection, out Vector3 _) < float.Epsilon)
                        {
                            int2 lineIndex = new int2(math.min(line.x, line.y), math.max(line.x, line.y));
                            if (intersectantLines.Add(lineIndex) == false)
                            {
                                intersectionCount--;
                            }
                        }
                    }

                    intersectionCount++;
                }
            }

            result[index] = intersectionCount % 2 == 0 ? 0 : 1;
        }

        public float3 IndexToLocal(int index)
        {
            int areaXY = resolution.x * resolution.y;
            int z = index / areaXY;
            int surplus = index % areaXY;
            int y = surplus / resolution.x;
            int x = surplus % resolution.x;
            float3 pixel = new float3(x, y, z);
            float3 normalLocal = pixel - (float3)(resolution - 1) / 2;
            return normalLocal * voxelSize + center;
        }
    }
}
