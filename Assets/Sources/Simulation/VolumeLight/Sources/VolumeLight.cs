using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class VolumeLight : SerializedMonoBehaviour, IProceduralTextureProcess
{
    [SerializeField] [ReadOnly] Shader shader;
    [SerializeField] IProceduralTextureCanvas positionTexture;
    [SerializeField] float maxDistance = 30;
    [SerializeField] float samplingCount = 15;
    [SerializeField] float lightPower = 1.3f;
    [SerializeField] float lightIntensity = 1;

    static readonly int MaxDistance = Shader.PropertyToID("_MaxDistance");
    static readonly int SamplingCount = Shader.PropertyToID("_SamplingCount");
    static readonly int LightPower = Shader.PropertyToID("_LightPower");
    static readonly int LightIntensity = Shader.PropertyToID("_LightIntensity");
    static readonly int UnityLightData = Shader.PropertyToID("unity_LightData");
    static readonly int PositionTexture = Shader.PropertyToID("_CameraPositionTexture");

    Material material;

    void OnEnable()
    {
        shader = Shader.Find("Hidden/VolumeLight");
        material = new Material(shader);
    }

    public void ProcessTexture(ScriptableRenderContext context, ref RenderingData renderingData,
        IProceduralTextureCanvas textureInfo, CommandBuffer cmd, RTHandle source, RTHandle destination)
    {
        cmd.SetGlobalTexture(PositionTexture, positionTexture.Texture);
        cmd.SetGlobalFloat(MaxDistance, maxDistance);
        cmd.SetGlobalFloat(SamplingCount, samplingCount);
        cmd.SetGlobalFloat(LightPower, lightPower);
        cmd.SetGlobalFloat(LightIntensity, lightIntensity);
        cmd.SetGlobalVector(UnityLightData,
            new Vector4(0, renderingData.lightData.additionalLightsCount, 1)); //Unity有时无法传递正确的灯光数量参数，不知道原因
        cmd.Blit(source, destination, material);
    }
}
