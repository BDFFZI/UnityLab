using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FullScreenPass : ScriptableRenderPass
{
    public RenderTargetIdentifier RenderTarget { get; set; }
    public Material Material { get; set; }

    public FullScreenPass(string name)
    {
        this.name = name;
    }

    readonly string name;

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer commandBuffer = CommandBufferPool.Get(name);

        commandBuffer.SetRenderTarget(RenderTarget);
        commandBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        commandBuffer.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, Material);

        context.ExecuteCommandBuffer(commandBuffer);
        context.Submit();
        CommandBufferPool.Release(commandBuffer);
    }
}
