using Unity.Mathematics;
using UnityEngine;

[ExecuteAlways]
public class ScanningMaterialRoot : MonoBehaviour
{
    [SerializeField] Material material;
    [SerializeField] float3 origin;
    [SerializeField] float distance;
    [SerializeField] float paddingDistance;
    [SerializeField] float paddingPower = 0.001f;
    [SerializeField] float radarRotation;
    [SerializeField] float3 lineDirection;

    static readonly int OriginID = Shader.PropertyToID("_Origin");
    static readonly int DistanceID = Shader.PropertyToID("_Distance");
    static readonly int RadarRotationID = Shader.PropertyToID("_RadarRotation");
    static readonly int LineDirectionID = Shader.PropertyToID("_LineDirection");
    static readonly int PaddingDistance = Shader.PropertyToID("_PaddingDistance");
    static readonly int PaddingPower = Shader.PropertyToID("_PaddingPower");

    MeshRenderer[] meshRenderers;

    void OnEnable()
    {
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        OnValidate();
    }

    void Update()
    {
        material.SetVector(OriginID, new float4(origin, 0));
        material.SetFloat(DistanceID, distance);
        material.SetFloat(PaddingDistance, paddingDistance);
        material.SetFloat(PaddingPower, paddingPower);
        material.SetFloat(RadarRotationID, radarRotation);
        material.SetVector(LineDirectionID, new float4(math.normalize(lineDirection), 0));
    }

    void OnValidate()
    {
        if (meshRenderers == null)
            return;

        foreach (MeshRenderer meshRenderer in meshRenderers)
            meshRenderer.material = material;
    }
}
