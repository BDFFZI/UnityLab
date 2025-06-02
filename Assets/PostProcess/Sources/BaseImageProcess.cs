using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class BaseImageProcess : MonoBehaviour
{
    BaseImageProcessPass baseImageProcessPass;

    void Start()
    {
        baseImageProcessPass = new BaseImageProcessPass();
        baseImageProcessPass.renderPassEvent = RenderPassEvent.AfterRendering;
    }
    void Update()
    {
        CustomPassFeature.RenderPasses.Add(baseImageProcessPass);
    }
}

public class BaseImageProcessPass : ScriptableRenderPass
{
    readonly Material material = new(Shader.Find("Hidden/BaseImageProcess"));
    RTHandle tempTarget;

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Debug.Log(renderingData.cameraData.cameraTargetDescriptor);
        // RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        // RenderingUtils.ReAllocateIfNeeded(ref tempTarget, descriptor);
        //
        // CommandBuffer commandBuffer = CommandBufferPool.Get("PostProcess");
        // commandBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        //
        // // Blitter.BlitCameraTexture(commandBuffer, renderingData.cameraData.renderer.cameraColorTargetHandle, tempTarget);
        // // Blitter.BlitCameraTexture(commandBuffer, tempTarget, renderingData.cameraData.renderer.cameraColorTargetHandle, material, 0);
        //
        // context.ExecuteCommandBuffer(commandBuffer);
        // CommandBufferPool.Release(commandBuffer);
    }
}
