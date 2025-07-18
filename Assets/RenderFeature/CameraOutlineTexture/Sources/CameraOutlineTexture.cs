using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(CameraNormalTexture), typeof(CameraPositionTexture))]
public class CameraOutlineTexture : RenderFeature<OutlineTexturePass>
{
    [SerializeField] bool preview;
    [SerializeField] float lineWidth = 1;
    [SerializeField] float depthEdgeThreshold = 0.25f;
    [SerializeField] float normalEdgeThreshold = 0.3f;
    [SerializeField] float edgeFaceThreshold = 0.58f;

    static readonly int LineWidthId = Shader.PropertyToID("_LineWidth");
    static readonly int DepthEdgeThresholdId = Shader.PropertyToID("_DepthEdgeThreshold");
    static readonly int NormalEdgeThresholdId = Shader.PropertyToID("_NormalEdgeThreshold");
    static readonly int EdgeFaceThresholdId = Shader.PropertyToID("_EdgeFaceThreshold");

    protected override void SetupPass(OutlineTexturePass pass)
    {
        base.SetupPass(pass);

        pass.Preview = preview;
        Shader.SetGlobalFloat(LineWidthId, lineWidth);
        Shader.SetGlobalFloat(DepthEdgeThresholdId, depthEdgeThreshold);
        Shader.SetGlobalFloat(NormalEdgeThresholdId, normalEdgeThreshold);
        Shader.SetGlobalFloat(EdgeFaceThresholdId, edgeFaceThreshold);
    }
}


public class OutlineTexturePass : RenderPass
{
    public bool Preview { get; set; }

    static readonly int CameraOutlineTextureId = Shader.PropertyToID("_CameraOutlineTexture");

    Material material;
    RTHandle renderTexture;

    public override void Awake()
    {
        base.Awake();
        material = new Material(Shader.Find("RenderFeature/CameraOutlineTexture"));
    }

    public override void OnDestroy()
    {
        RTHandles.Release(renderTexture);
        base.OnDestroy();
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        RenderTextureDescriptor renderTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        renderTargetDescriptor.depthBufferBits = 0;
        renderTargetDescriptor.useMipMap = true;
        renderTargetDescriptor.autoGenerateMips = true;
        RenderingUtils.ReAllocateIfNeeded(ref renderTexture, renderTargetDescriptor, FilterMode.Trilinear, TextureWrapMode.Clamp);

        CommandBuffer cmd = CommandBufferPool.Get("CameraOutlineTexture");
        cmd.Blit(null, renderTexture, material, 0);
        if (Preview)
            cmd.Blit(renderTexture, renderingData.cameraData.renderer.cameraColorTargetHandle);
        cmd.SetGlobalTexture(CameraOutlineTextureId, renderTexture);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
