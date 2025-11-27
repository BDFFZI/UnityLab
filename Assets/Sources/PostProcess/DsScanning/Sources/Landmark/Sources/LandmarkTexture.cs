using UnityEngine;
using UnityEngine.Rendering;

public class LandmarkTexture : RenderTextureFeature<LandmarkTexturePassByMaterial>
{
    [SerializeField] RenderTexture renderTexture;
    [SerializeField] float positionY = 0;
    [SerializeField] float positionYThreshold = 0.5f;
    [SerializeField] Vector3 direction = new Vector3(0, 1, 0);
    [SerializeField] float directionThreshold = 0.9f;
    [SerializeField] Vector2 distanceThreshold = new Vector2(0, 20);
    [SerializeField] float pointMaskScale = 4;

    protected override void SetupPass(LandmarkTexturePassByMaterial passByMaterial)
    {
        base.SetupPass(passByMaterial);

        passByMaterial.Material.SetFloat("_PositionY", positionY);
        passByMaterial.Material.SetFloat("_PositionYThreshold", positionYThreshold);
        passByMaterial.Material.SetVector("_Direction", direction);
        passByMaterial.Material.SetFloat("_DirectionThreshold", directionThreshold);
        passByMaterial.Material.SetVector("_Origin", transform.position);
        passByMaterial.Material.SetVector("_DistanceThreshold", distanceThreshold);
        passByMaterial.Material.SetFloat("_PointMaskScale", pointMaskScale);
    }
}

public class LandmarkTexturePassByMaterial : RenderTexturePassByMaterial
{
    public override Material Material { get; } = new Material(Shader.Find("RenderFeature/LandmarkTexture"));
}
