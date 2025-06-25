using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BoxBlur : PostProcess<BoxBlurPass>
{
    public enum BoxType
    {
        _2x2 = 0,
        _3x3 = 1
    }

    [SerializeField] [Range(1, 4)] int downsample = 2;
    [SerializeField] [Range(0, 8)] int blurIterations = 1;
    [SerializeField] int blurRadius = 1;
    [SerializeField] BoxType blurType = BoxType._2x2;

    protected override void SetupPass(BoxBlurPass pass)
    {
        pass.Downsample = downsample;
        pass.BlurIterations = blurIterations;
        pass.BlurRadius = blurRadius;
        pass.PassIndex = (int)blurType;
    }
}

public class BoxBlurPass : PostProcessPass
{
    public int Downsample { get; set; }
    public int BlurIterations { get; set; }
    public int BlurRadius { get; set; }
    public int PassIndex { get; set; }


    Material material = new(Shader.Find("Hidden/BoxBlur"));
    RTHandle rtHandle1;
    RTHandle rtHandle2;

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        //创建纹理
        RenderTextureDescriptor cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        RenderTextureDescriptor textureDescriptor = new RenderTextureDescriptor(cameraDescriptor.width / Downsample, cameraDescriptor.height / Downsample, cameraDescriptor.colorFormat);
        RenderingUtils.ReAllocateIfNeeded(ref rtHandle1, textureDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp);
        RenderingUtils.ReAllocateIfNeeded(ref rtHandle2, textureDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp);

        material.SetInt("_BlurRadius", BlurRadius / Downsample);

        CommandBuffer cmd = CommandBufferPool.Get("BoxBlur");

        cmd.Blit(renderingData.cameraData.renderer.cameraColorTargetHandle, rtHandle1);
        for (int i = 0; i < BlurIterations; i++)
        {
            cmd.Blit(rtHandle1, rtHandle2, material, PassIndex);
            cmd.Blit(rtHandle2, rtHandle1, material, PassIndex);
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
