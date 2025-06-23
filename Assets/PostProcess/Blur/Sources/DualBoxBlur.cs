using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DualBoxBlur : PostProcess<DualBoxBlurPass>
{
    [SerializeField] [Range(1, 4)] int downsample = 2;
    [SerializeField] [Range(0, 8)] int blurIterations = 1;
    [SerializeField] int blurRadius = 1;

    protected override void OnBeginCameraRendering(ScriptableRenderContext arg1, Camera arg2)
    {
        PostProcessPass.Downsample = downsample;
        PostProcessPass.BlurIterations = blurIterations;
        PostProcessPass.BlurRadius = blurRadius;
        base.OnBeginCameraRendering(arg1, arg2);
    }
}

public class DualBoxBlurPass : PostProcessPass
{
    public int Downsample { get; set; }
    public int BlurIterations { get; set; }
    public int BlurRadius { get; set; }

    Material material = new(Shader.Find("Hidden/BoxBlur"));

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        //配置材质球
        material.SetInt("_BlurRadius", BlurRadius / Downsample);

        //创建纹理
        RenderTextureDescriptor cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        int width = cameraDescriptor.width / Downsample;
        int height = cameraDescriptor.height / Downsample;
        RenderTexture rtHandle1 = RenderTexture.GetTemporary(width, height, 0, cameraDescriptor.colorFormat);
        RenderTexture rtHandle2 = RenderTexture.GetTemporary(width, height, 0, cameraDescriptor.colorFormat);

        CommandBuffer cmd = CommandBufferPool.Get("DualBoxBlur");
        cmd.Blit(renderingData.cameraData.renderer.cameraColorTargetHandle, rtHandle1);
        //模糊算法
        {
            for (int i = 0; i < BlurIterations; i++)
            {
                RenderTexture.ReleaseTemporary(rtHandle2);
                width /= 2;
                height /= 2;
                rtHandle2 = RenderTexture.GetTemporary(width, height, 0, cameraDescriptor.colorFormat);
                cmd.Blit(rtHandle1, rtHandle2, material, 0);

                RenderTexture.ReleaseTemporary(rtHandle1);
                width /= 2;
                height /= 2;
                rtHandle1 = RenderTexture.GetTemporary(width, height, 0, cameraDescriptor.colorFormat);
                cmd.Blit(rtHandle2, rtHandle1, material, 0);
            }
            for (int i = 0; i < BlurIterations; i++)
            {
                RenderTexture.ReleaseTemporary(rtHandle2);
                width *= 2;
                height *= 2;
                rtHandle2 = RenderTexture.GetTemporary(width, height, 0, cameraDescriptor.colorFormat);
                cmd.Blit(rtHandle1, rtHandle2, material, 0);

                RenderTexture.ReleaseTemporary(rtHandle1);
                width *= 2;
                height *= 2;
                rtHandle1 = RenderTexture.GetTemporary(width, height, 0, cameraDescriptor.colorFormat);
                cmd.Blit(rtHandle2, rtHandle1, material, 0);
            }
        }
        cmd.Blit(rtHandle1, renderingData.cameraData.renderer.cameraColorTargetHandle);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);

        //回收纹理
        RenderTexture.ReleaseTemporary(rtHandle1);
        RenderTexture.ReleaseTemporary(rtHandle2);
    }
}
