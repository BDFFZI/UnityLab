using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

[DefaultExecutionOrder(-10)]
public class MarchingCubesWorld : MonoBehaviour
{
    public Texture3D OriginalWorld { get => originalWorld; set => originalWorld = value; }
    public bool IsDirty { get; set; } = true;
    public event Action OnUpdate;
    public event Action OnUpdated;

    public RenderTexture World => world;
    public int3 WorldResolution => worldResolution;
    public int3 WorldDimension => worldDimension;
    public int WorldVolume => worldVolume;
    public float WorldVisibleDensity => worldVisibleDensity;

    public float3 TransformArrayToLocal(int3 arrayIndex)
    {
        return arrayIndex - worldDimension / 2;
    }
    public int3 TransformWorldToArray(float3 worldPos)
    {
        float3 localPos = transform.InverseTransformPoint(worldPos);
        return (int3)(localPos + worldDimension / 2 + 0.01f); //弥补部分浮点数误差，强制转换是向下取整，只要worldPoint是来自采样点的坐标，就不会有问题
    }

    [SerializeField] Texture3D originalWorld;
    [SerializeField] ComputeShader worldFilter;
    [SerializeField] float worldVisibleDensity = 0.1f;
    [SerializeField] int fuzzyRadius = 1;
    [SerializeField] float variance = 1;

    int3 worldResolution;
    int3 worldDimension;
    int worldVolume;
    RenderTexture world;

    Texture3D gaussianDistribution;

    void Start()
    {
        originalWorld = Instantiate(originalWorld); //克隆世界信息，避免修改原文件
        worldResolution = new int3(originalWorld.width, originalWorld.height, originalWorld.depth);
        worldDimension = worldResolution - 1;
        worldVolume = worldDimension.x * worldDimension.y * worldDimension.z;
        //使用经过后处理的世界
        world = new RenderTexture(worldResolution.x, worldResolution.y, 0, originalWorld.graphicsFormat);
        world.dimension = TextureDimension.Tex3D;
        world.volumeDepth = worldResolution.z;
        world.enableRandomWrite = true;
        world.Create();
        //高斯模糊
        {
            float total = 0;
            gaussianDistribution = new Texture3D(fuzzyRadius * 2 + 1, fuzzyRadius * 2 + 1, fuzzyRadius * 2 + 1, GraphicsFormat.R32_SFloat, TextureCreationFlags.None);
            for (int x = -fuzzyRadius; x <= fuzzyRadius; x++)
            for (int y = -fuzzyRadius; y <= fuzzyRadius; y++)
            for (int z = -fuzzyRadius; z <= fuzzyRadius; z++)
            {
                double p = math.pow(2.71828, -(math.lengthsq(new float3(x, y, z)) / (2 * variance))) / math.pow(2 * math.PI * variance, 3 / 2f);
                total += (float)p;
                gaussianDistribution.SetPixel(x + fuzzyRadius, y + fuzzyRadius, z + fuzzyRadius, new Color((float)p, 0, 0));
            }

            for (int x = -fuzzyRadius; x <= fuzzyRadius; x++)
            for (int y = -fuzzyRadius; y <= fuzzyRadius; y++)
            for (int z = -fuzzyRadius; z <= fuzzyRadius; z++)
            {
                float p = gaussianDistribution.GetPixel(x, y, z).r;
                gaussianDistribution.SetPixel(x, y, z, new Color(p / total, 0, 0));
            }

            gaussianDistribution.Apply();
        }
    }

    void Update()
    {
        if (IsDirty)
        {
            OnUpdate?.Invoke();
            IsDirty = false;
            originalWorld.Apply();
            worldFilter.SetTexture(0, "_Input", originalWorld);
            worldFilter.SetTexture(0, "_Output", world);
            worldFilter.SetTexture(0, "_GaussianDistribution", gaussianDistribution);
            worldFilter.SetInt("_FuzzyRadius", fuzzyRadius);
            worldFilter.Dispatch(0, worldResolution.x, worldResolution.y, worldResolution.z);
            OnUpdated?.Invoke();
        }
    }
}

[CustomEditor(typeof(MarchingCubesWorld))]
public class MarchingCubesEditor : Editor
{
    bool showHandles;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        showHandles = GUILayout.Toggle(showHandles, "Show Handles");
    }

    void OnSceneGUI()
    {
        if (showHandles)
        {
            MarchingCubesWorld marchingCubes = (MarchingCubesWorld)target;
            Handles.matrix = marchingCubes.transform.localToWorldMatrix;
            foreach (Transform child in marchingCubes.transform)
            {
                int3 arrayIndex = marchingCubes.TransformWorldToArray(child.position);
                float pointValue = marchingCubes.OriginalWorld.GetPixel(arrayIndex.x, arrayIndex.y, arrayIndex.z).r;

                Handles.color = new Color(pointValue, pointValue, pointValue, 1);
                Handles.DrawWireCube(child.localPosition, new float3(0.1f));
            }
        }
    }
}
