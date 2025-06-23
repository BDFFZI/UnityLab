using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SimplePostProcessPass : PostProcessPass
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

    public override void Dispose()
    {
        RTHandles.Release(tempTarget);
    }
}
