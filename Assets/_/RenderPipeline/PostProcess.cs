using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class PostProcess : MonoBehaviour
{
    public Material Material { get => material; set => material = value; }

    [SerializeField] Material material;

    new Camera camera;
    PostProcessPass postProcessPass;

    protected virtual void OnEnable()
    {
        camera = GetComponent<Camera>();
        postProcessPass = new PostProcessPass();
        postProcessPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    protected virtual void OnBeginCameraRendering(ScriptableRenderContext arg1, Camera arg2)
    {
        if (camera != arg2 || material == null)
            return;

        postProcessPass.Material = material;
        CustomRendererFeature.RenderPasses.Add(postProcessPass);
    }
}

public class PostProcessPass : ScriptableRenderPass
{
    public Material Material { get; set; }

    RTHandle tempTarget;

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        RenderingUtils.ReAllocateIfNeeded(ref tempTarget,
            new RenderTextureDescriptor(descriptor.width, descriptor.height, descriptor.colorFormat), FilterMode.Bilinear, TextureWrapMode.Clamp,
            name: "_TempTarget"
        );

        CommandBuffer commandBuffer = CommandBufferPool.Get("PostProcess");
        commandBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        commandBuffer.Blit(renderingData.cameraData.renderer.cameraColorTargetHandle, tempTarget);
        commandBuffer.Blit(tempTarget, renderingData.cameraData.renderer.cameraColorTargetHandle, Material, 0);
        context.ExecuteCommandBuffer(commandBuffer);
        CommandBufferPool.Release(commandBuffer);
    }
}
