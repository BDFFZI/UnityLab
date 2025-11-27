using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// GPU版本是直接将体素信息交给渲染着色器，再有其通过几何着色器生成的视觉效果。
/// </summary>
public class MarchingCubesGPU : MonoBehaviour
{
    [SerializeField] MarchingCubesWorld world;
    [SerializeField] bool generateCollider;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    Transform colliderCollector;
    readonly Dictionary<int3, GameObject> colliders = new Dictionary<int3, GameObject>();

    void Start()
    {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        //激活几何着色器
        Mesh mesh = new Mesh();
        mesh.SetVertices(new Vector3[1]);
        mesh.SetIndices(new int[world.WorldVolume], MeshTopology.Points, 0);
        mesh.bounds = new Bounds(Vector3.zero, (float3)world.WorldDimension);
        meshFilter.mesh = mesh;
        //使用特制材质渲染
        Material material = new Material(Shader.Find("Unlit/MarchingCubesGPU"));
        material.SetTexture("_World", world.World);
        material.SetVector("_WorldResolution", new float4(world.WorldResolution, 0));
        material.SetVector("_WorldDimension", new float4(world.WorldDimension, 0));
        material.SetFloat("_WorldVisibleDensity", world.WorldVisibleDensity);
        material.SetMatrix("_WorldMatrixM", transform.localToWorldMatrix);
        meshRenderer.material = material;
        //生成碰撞
        if (generateCollider)
        {
            colliderCollector = new GameObject("Colliders").transform;
            colliderCollector.gameObject.layer = gameObject.layer;
            colliderCollector.SetParent(transform, false);
            for (int x = 0; x < world.OriginalWorld.width; x++)
            for (int y = 0; y < world.OriginalWorld.height; y++)
            for (int z = 0; z < world.OriginalWorld.depth; z++)
            {
                float density = world.OriginalWorld.GetPixel(x, y, z).r;
                if (density >= world.WorldVisibleDensity)
                {
                    int3 pixel = new int3(x, y, z);

                    GameObject collider = new GameObject(pixel.ToString(), typeof(BoxCollider));
                    collider.layer = colliderCollector.gameObject.layer;

                    Transform colliderTransform = collider.transform;
                    colliderTransform.SetParent(colliderCollector, false);
                    colliderTransform.localPosition = world.TransformArrayToLocal(new int3(x, y, z));

                    colliders[pixel] = collider;
                }
            }

            world.OnUpdated += () => {
                int3 resolution = world.WorldResolution;
                for (int x = resolution.x - 1; x >= 0; x--)
                for (int y = resolution.y - 1; y >= 0; y--)
                for (int z = resolution.z - 1; z >= 0; z--)
                {
                    float density = world.OriginalWorld.GetPixel(x, y, z).r;
                    int3 pixel = new int3(x, y, z);
                    if (density < world.WorldVisibleDensity && colliders.ContainsKey(pixel))
                    {
                        Destroy(colliders[pixel]);
                        colliders.Remove(pixel);
                    }
                }
            };
        }
    }
}
