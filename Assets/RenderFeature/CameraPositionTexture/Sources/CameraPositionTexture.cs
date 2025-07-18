using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraPositionTexture : RenderFeature<CameraPositionTexturePass>
{
    [SerializeField] bool preview;

    static readonly int ClipToWorldDirID = Shader.PropertyToID("_ClipToWorld");

    protected override void SetupPass(CameraPositionTexturePass pass)
    {
        base.SetupPass(pass);

        pass.Preview = preview;
        Shader.SetGlobalMatrix(ClipToWorldDirID, Matrix4x4.Inverse(GL.GetGPUProjectionMatrix(Camera.projectionMatrix, false) * Camera.worldToCameraMatrix));
    }
}

public class CameraPositionTexturePass : RenderPass
{
    public bool Preview { get; set; }

    static readonly int CameraPositionTextureID = Shader.PropertyToID("_CameraPositionTexture");

    Material material;
    RTHandle renderTarget;

    public override void Awake()
    {
        base.Awake();

        material = new Material(Shader.Find("RenderFeature/CameraPositionTexture"));
    }

    public override void OnDestroy()
    {
        RTHandles.Release(renderTarget);

        base.OnDestroy();
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        base.Configure(cmd, cameraTextureDescriptor);

        cameraTextureDescriptor.depthBufferBits = 0;
        cameraTextureDescriptor.colorFormat = RenderTextureFormat.ARGBFloat;
        RenderingUtils.ReAllocateIfNeeded(ref renderTarget, cameraTextureDescriptor, name: "_CameraPositionTexture");

        ConfigureTarget(renderTarget);
        ConfigureClear(ClearFlag.All, Color.clear);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("CameraPositionTexture");

        cmd.Blit(null, renderTarget, material, 0);
        if (Preview)
            cmd.Blit(renderTarget, renderingData.cameraData.renderer.cameraColorTargetHandle);
        cmd.SetGlobalTexture(CameraPositionTextureID, renderTarget);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
