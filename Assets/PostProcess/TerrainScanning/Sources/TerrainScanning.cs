using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TerrainScanning : SimplePostProcess
{
    protected override void SetupPass(SimplePostProcessPass pass)
    {
        base.SetupPass(pass);
        pass.Material.SetMatrix("_ClipToWorld", Matrix4x4.Inverse(GL.GetGPUProjectionMatrix(Camera.projectionMatrix, true) * Camera.worldToCameraMatrix));
        pass.Material.SetVector("_CameraPosition", Camera.transform.position);
    }

    // public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    // {
    //     RenderTextureDescriptor textureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
    //     RenderTexture flow = RenderTexture.GetTemporary(textureDescriptor.width, textureDescriptor.height);
    //     RenderTexture 
    //     
    //     
    //     CommandBuffer command = CommandBufferPool.Get();
    //
    //
    //
    //     context.ExecuteCommandBuffer(command);
    //     CommandBufferPool.Release(command);
    // }
}
