using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GaussianBlur : PostProcess<GaussianBlurPass>
{
    [SerializeField] [Range(1, 4)] int downsample = 2;
    [SerializeField] [Range(0, 8)] int blurIterations = 1;
    [SerializeField] int blurRadius = 1;


    protected override void SetupPass(GaussianBlurPass pass)
    {
        pass.Downsample = downsample;
        pass.BlurIterations = blurIterations;
        pass.BlurRadius = blurRadius;
    }
}

public class GaussianBlurPass : PostProcessPass
{
    public int Downsample { get; set; }
    public int BlurIterations { get; set; }
    public int BlurRadius { get; set; }


    Material material = new(Shader.Find("Hidden/GaussianBlur"));
    RTHandle rtHandle1;
    RTHandle rtHandle2;

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        //创建纹理
        RenderTextureDescriptor cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        RenderTextureDescriptor textureDescriptor = new RenderTextureDescriptor(cameraDescriptor.width / Downsample, cameraDescriptor.height / Downsample, cameraDescriptor.colorFormat);
        RenderingUtils.ReAllocateIfNeeded(ref rtHandle1, textureDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp);
        RenderingUtils.ReAllocateIfNeeded(ref rtHandle2, textureDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp);

        float unitWidth = 1.0f / textureDescriptor.width * ((float)BlurRadius / Downsample);
        float unitHeight = 1.0f / textureDescriptor.height * ((float)BlurRadius / Downsample);
        material.SetVector("_BlurOffset", new Vector4(unitWidth, -unitWidth, unitHeight, -unitHeight));

        CommandBuffer cmd = CommandBufferPool.Get("GaussianBlur");

        cmd.Blit(renderingData.cameraData.renderer.cameraColorTargetHandle, rtHandle1);
        for (int i = 0; i < BlurIterations; i++)
        {
            cmd.Blit(rtHandle1, rtHandle2, material, 0);
            cmd.Blit(rtHandle2, rtHandle1, material, 1);
        }
        cmd.Blit(rtHandle1, renderingData.cameraData.renderer.cameraColorTargetHandle);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void Dispose()
    {
        RTHandles.Release(rtHandle1);
        RTHandles.Release(rtHandle2);
        base.Dispose();
    }
}
