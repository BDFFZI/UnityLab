using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class MarchingCubesBlade : MonoBehaviour
{
    [ContextMenu("Separate")]
    public void Separate()
    {
        int count = Texture3DAlgorithm.SeparatedConnectedComponent(marchingCubes.OriginalWorld, marchingCubes.WorldVisibleDensity, separated);
        if (count != 0)
            marchingCubes.IsDirty = true;
        if (count > 10)
        {
            Debug.Log(count);
            MarchingCubesWorld marchingCubesWorld = Instantiate(separatedPrefab, marchingCubes.transform);
            marchingCubesWorld.OriginalWorld = Instantiate(separated);
            ClearSeparated();
        }
    }

    [SerializeField] MarchingCubesWorld marchingCubes;
    [SerializeField] MarchingCubesWorld separatedPrefab;

    Texture3D separated;

    void ClearSeparated()
    {
        NativeArray<float> pixel = separated.GetPixelData<float>(0);
        for (int i = 0; i < pixel.Length; i++) pixel[i] = 0;
    }

    void Awake()
    {
        separated = new Texture3D(
            marchingCubes.OriginalWorld.width, marchingCubes.OriginalWorld.height, marchingCubes.OriginalWorld.depth,
            marchingCubes.OriginalWorld.graphicsFormat, TextureCreationFlags.None);
        separated.filterMode = FilterMode.Bilinear;
        ClearSeparated();
    }

    void OnTriggerStay(Collider other)
    {
        int3 arrayIndex = marchingCubes.TransformWorldToArray(other.transform.position);
        if (other.gameObject.name == arrayIndex.ToString())
        {
            float feature = marchingCubes.OriginalWorld.GetPixel(arrayIndex.x, arrayIndex.y, arrayIndex.z).r;
            feature = math.max(feature - 1f, 0);
            marchingCubes.OriginalWorld.SetPixel(arrayIndex.x, arrayIndex.y, arrayIndex.z, new Color(feature, 0, 0, 0));
            marchingCubes.IsDirty = true;
        }
    }

    void Update()
    {
        Separate();
    }
}
