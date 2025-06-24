using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Bloom : PostProcess<BloomPass>
{

}

public class BloomPass : PostProcessPass
{
    Material material = new Material(Shader.Find("Hidden/Bloom"));

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        RenderTextureDescriptor renderTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        RenderTexture tempTarget = RenderTexture.GetTemporary(renderTextureDescriptor.width, renderTextureDescriptor.height);

        CommandBuffer command = CommandBufferPool.Get("Bloom");

        command.Blit(renderingData.cameraData.renderer.cameraColorTargetHandle, tempTarget, material, 0);
        command.Blit(tempTarget, renderingData.cameraData.renderer.cameraColorTargetHandle);

        context.ExecuteCommandBuffer(command);
        CommandBufferPool.Release(command);
    }
}
