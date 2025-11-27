using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class Fog : MonoBehaviour, IProceduralTextureProcess
{
    [SerializeField] [ReadOnly] Shader shader;
    [SerializeField] [Required] CameraPositionTexture positionTexture;
    [SerializeField] Texture2D colorTex;
    [SerializeField] Color color = Color.white;
    [SerializeField] float depthStart = 100;
    [SerializeField] float depthEnd = 600;
    [SerializeField] float heightStart;
    [SerializeField] float heightEnd = 600;
    [SerializeField] float skyHeightStart = 50;
    [SerializeField] float skyHeightEnd = 300;
    [SerializeField] float sunScattering = 10;
    [SerializeField] float sunIntensity = 0.5f;

    static readonly int PositionTexture = Shader.PropertyToID("_CameraPositionTexture");
    static readonly int ColorTexID = Shader.PropertyToID("_ColorTex");
    static readonly int ColorID = Shader.PropertyToID("_Color");
    static readonly int DepthStartID = Shader.PropertyToID("_DepthStart");
    static readonly int DepthEndID = Shader.PropertyToID("_DepthEnd");
    static readonly int HeightStartID = Shader.PropertyToID("_HeightStart");
    static readonly int HeightEndID = Shader.PropertyToID("_HeightEnd");
    static readonly int SkyHeightStartID = Shader.PropertyToID("_SkyHeightStart");
    static readonly int SkyHeightEndID = Shader.PropertyToID("_SkyHeightEnd");
    static readonly int SunScatteringID = Shader.PropertyToID("_SunScattering");
    static readonly int SunIntensityID = Shader.PropertyToID("_SunIntensity");

    Material material;

    void OnEnable()
    {
        shader = Shader.Find("Hidden/Fog");
        material = new Material(shader);
    }

    public void ProcessTexture(ScriptableRenderContext context, ref RenderingData renderingData,
        IProceduralTextureCanvas textureInfo,
        CommandBuffer cmd, RTHandle source, RTHandle destination)
    {
        if (positionTexture == null)
            return;

        material.SetTexture(ColorTexID, colorTex ?? Texture2D.whiteTexture);
        cmd.SetGlobalTexture(PositionTexture, positionTexture.Texture);
        cmd.SetGlobalColor(ColorID, color.linear);
        cmd.SetGlobalFloat(DepthStartID, depthStart);
        cmd.SetGlobalFloat(DepthEndID, depthEnd);
        cmd.SetGlobalFloat(HeightStartID, heightStart);
        cmd.SetGlobalFloat(HeightEndID, heightEnd);
        cmd.SetGlobalFloat(SkyHeightStartID, skyHeightStart);
        cmd.SetGlobalFloat(SkyHeightEndID, skyHeightEnd);
        cmd.SetGlobalFloat(SunScatteringID, sunScattering);
        cmd.SetGlobalFloat(SunIntensityID, sunIntensity);
        cmd.Blit(source, destination, material, 0);
    }
}
