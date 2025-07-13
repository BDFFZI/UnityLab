using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[CreateAssetMenu(menuName = "MarchingCubesBuilder")]
public class Texture3DEditor : ScriptableObject
{
    public enum BuildType
    {
        Null,
        Box,
        Sphere,
        Mesh,
    }

    public int resolution;
    public BuildType buildType;
    public Mesh mesh;
}

[CustomEditor(typeof(Texture3DEditor))]
public class Texture3DEditorEditor : Editor
{
    void BuildNull(Texture3D world)
    {
        for (int x = 0; x < world.width; x++)
        for (int y = 0; y < world.height; y++)
        for (int z = 0; z < world.depth; z++)
        {
            world.SetPixel(x, y, z, new Color());
        }
    }
    void BuildBox(Texture3D world)
    {
        for (int x = 0; x < world.width; x++)
        for (int y = 0; y < world.height; y++)
        for (int z = 0; z < world.depth; z++)
        {
            bool isInside = x != 0 && x != world.width - 1 && y != 0 && y != world.height - 1 && z != 0 && z != world.depth - 1;
            float value = isInside ? 1 : 0;
            world.SetPixel(x, y, z, new Color(value, value, value, value));
        }
    }
    void BuildSphere(Texture3D world)
    {
        Vector3 center = new Vector3((world.width - 1) / 2.0f, (world.height - 1) / 2.0f, (world.depth - 1) / 2.0f);
        float radius = (Mathf.Min(world.width, world.height, world.depth) - 1) / 2.0f;
        for (int x = 0; x < world.width; x++)
        for (int y = 0; y < world.height; y++)
        for (int z = 0; z < world.depth; z++)
        {
            Vector3 pos = new Vector3(x, y, z);
            float distance = Vector3.Distance(pos, center);
            float value = 1 - Mathf.Clamp01(distance / radius);
            world.SetPixel(x, y, z, new Color(value, value, value, value));
        }
    }
    void BuildMesh(out Texture3D world, Mesh mesh, int precision)
    {
        world = Texture3DAlgorithm.MeshToWorld(mesh, precision);
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Texture3DEditor mapBuilder = (Texture3DEditor)target;
        if (GUILayout.Button("CreateWorld"))
        {
            int3 textureResolution = mapBuilder.resolution;
            Texture3D world = new Texture3D(
                textureResolution.x, textureResolution.y, textureResolution.z,
                GraphicsFormat.R32_SFloat, TextureCreationFlags.None
            );


            string name = "NewWorld";
            switch (mapBuilder.buildType)
            {
                case Texture3DEditor.BuildType.Null:
                    BuildNull(world);
                    break;
                case Texture3DEditor.BuildType.Box:
                    BuildBox(world);
                    break;
                case Texture3DEditor.BuildType.Sphere:
                    BuildSphere(world);
                    break;
                case Texture3DEditor.BuildType.Mesh:
                    name = mapBuilder.mesh.name;
                    BuildMesh(out world, mapBuilder.mesh, mapBuilder.resolution);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            world.filterMode = FilterMode.Bilinear;
            world.wrapMode = TextureWrapMode.Clamp;
            world.Apply();
            Debug.Log("纹理分辨率：" + new int3(world.width, world.height, world.depth));

            string builderPath = AssetDatabase.GetAssetPath(mapBuilder);
            string worldPath = builderPath.Substring(0, builderPath.LastIndexOf('/')) + $"/{name}.asset";
            AssetDatabase.CreateAsset(world, worldPath);
            AssetDatabase.SaveAssets();
        }
    }
}
