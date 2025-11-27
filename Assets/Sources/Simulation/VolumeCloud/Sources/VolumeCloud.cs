using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class VolumeCloud : MonoBehaviour, IProceduralTextureProcess
{
    [SerializeField] [ReadOnly] Shader shader;
    [SerializeField] Texture3D densityTex;
    [SerializeField] float3 densityTexPosition = new float3(0, 100, 0);
    [SerializeField] float3 densityTexBound = new float3(2000, 1500, 2000);
    [SerializeField] [Range(0, 1)] float densityThreshold = 0.05f;
    [SerializeField] float densityPower = 0.2f;
    [SerializeField] float densityScale = 0.2f;
    [SerializeField] Texture2D samplingNoiseTex;
    [SerializeField] float lightOutMaxDistance = 400;
    [SerializeField] int lightOutSamplingCount = 80;
    [SerializeField] float lightInMaxDistance = 20;
    [SerializeField] int lightInSamplingCount = 4;
    [SerializeField] float3 windSpeed = new float3(0.01f, 0, 0.01f);
    [SerializeField] Color cloudColor = Color.white;
    [SerializeField] float cloudAlphaPower = 1;

    static readonly int ClipToWorldOpenGL = Shader.PropertyToID("_ClipToWorld_OpenGL");
    static readonly int DensityTex = Shader.PropertyToID("_DensityTex");
    static readonly int DensityTexPosition = Shader.PropertyToID("_DensityTexPosition");
    static readonly int DensityTexBound = Shader.PropertyToID("_DensityTexBound");
    static readonly int LightOutMaxDistance = Shader.PropertyToID("_LightOutMaxDistance");
    static readonly int LightOutSamplingCount = Shader.PropertyToID("_LightOutSamplingCount");
    static readonly int LightInMaxDistance = Shader.PropertyToID("_LightInMaxDistance");
    static readonly int LightInSamplingCount = Shader.PropertyToID("_LightInSamplingCount");
    static readonly int DensityThreshold = Shader.PropertyToID("_DensityThreshold");
    static readonly int DensityScale = Shader.PropertyToID("_DensityScale");
    static readonly int WindSpeed = Shader.PropertyToID("_WindSpeed");
    static readonly int CloudColor = Shader.PropertyToID("_CloudColor");
    static readonly int SamplingNoiseTex = Shader.PropertyToID("_SamplingNoiseTex");
    static readonly int DensityPower = Shader.PropertyToID("_DensityPower");
    static readonly int CloudAlphaPower = Shader.PropertyToID("_CloudAlphaPower");

    Material material;

    void OnEnable()
    {
        shader = Shader.Find("Hidden/VolumeCloud");
        material = new Material(shader);
    }

    public void ProcessTexture(ScriptableRenderContext context, ref RenderingData renderingData,
        IProceduralTextureCanvas textureInfo, CommandBuffer cmd, RTHandle source, RTHandle destination)
    {
        Camera camera = renderingData.cameraData.camera;

        cmd.SetGlobalMatrix(ClipToWorldOpenGL, math.inverse(camera.projectionMatrix * camera.worldToCameraMatrix));
        cmd.SetGlobalTexture(DensityTex, densityTex);
        cmd.SetGlobalVector(DensityTexPosition, new float4(densityTexPosition, 0));
        cmd.SetGlobalVector(DensityTexBound, new float4(densityTexBound, 0));
        cmd.SetGlobalFloat(DensityThreshold, densityThreshold);
        cmd.SetGlobalFloat(DensityPower, densityPower);
        cmd.SetGlobalFloat(DensityScale, densityScale);
        cmd.SetGlobalTexture(SamplingNoiseTex, samplingNoiseTex);
        cmd.SetGlobalFloat(LightOutMaxDistance, lightOutMaxDistance);
        cmd.SetGlobalInt(LightOutSamplingCount, lightOutSamplingCount);
        cmd.SetGlobalFloat(LightInMaxDistance, lightInMaxDistance);
        cmd.SetGlobalInt(LightInSamplingCount, lightInSamplingCount);
        cmd.SetGlobalVector(WindSpeed, new float4(windSpeed, 0));
        cmd.SetGlobalVector(CloudColor, cloudColor);
        cmd.SetGlobalFloat(CloudAlphaPower, cloudAlphaPower);
        cmd.Blit(source, destination, material);
    }
}
