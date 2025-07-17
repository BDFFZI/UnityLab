using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DualBoxBlur : PostProcess<DualBoxBlurPass>
{
    [SerializeField] [Range(1, 4)] int downsample = 2;
    [SerializeField] [Range(0, 8)] int blurIterations = 1;
    [SerializeField] int blurRadius = 1;
    [SerializeField] string customRenderTarget;

    protected override void SetupPass(DualBoxBlurPass pass)
    {
        base.SetupPass(pass);
        pass.Downsample = downsample;
        pass.BlurIterations = blurIterations;
        pass.BlurRadius = blurRadius;
        pass.CustomRenderTargetID = customRenderTarget;
    }
}

public class DualBoxBlurPass : RenderPass
{
    static readonly int BlurRadiusID = Shader.PropertyToID("_BlurRadius");

    public int Downsample { get; set; }
    public int BlurIterations { get; set; }
    public int BlurRadius { get; set; }

    public string CustomRenderTargetID { get; set; }

    readonly Material material = new(Shader.Find("Hidden/BoxBlur"));
    readonly List<RenderTexture> tempTextures = new List<RenderTexture>();
    RTHandle customRenderTarget;


    void CreateTempTextures(RenderTextureDescriptor renderTextureDescriptor)
    {
        int width = renderTextureDescriptor.width / Downsample;
        int height = renderTextureDescriptor.height / Downsample;

        RenderTexture initialTex = RenderTexture.GetTemporary(width, height, 0, renderTextureDescriptor.colorFormat);
        tempTextures.Add(initialTex);

        for (int i = 0; i < BlurIterations; i++)
        {
            width /= 2;
            height /= 2;
            tempTextures.Add(RenderTexture.GetTemporary(width, height, 0, renderTextureDescriptor.colorFormat));
        }
        for (int i = 0; i < BlurIterations; i++)
        {
            width *= 2;
            height *= 2;
            tempTextures.Add(RenderTexture.GetTemporary(width, height, 0, renderTextureDescriptor.colorFormat));
        }
    }
    void ReleaseTempTextures()
    {
        foreach (RenderTexture tempTexture in tempTextures)
            RenderTexture.ReleaseTemporary(tempTexture);
        tempTextures.Clear();
    }


    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        //配置材质球
        material.SetInt(BlurRadiusID, BlurRadius / Downsample);

        //创建临时纹理
        RenderTextureDescriptor targetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        CreateTempTextures(targetDescriptor);

        //执行模糊算法
        {
            CommandBuffer command = CommandBufferPool.Get("DualBoxBlur");

            //将当前画面传入处理槽
            command.Blit(renderingData.cameraData.renderer.cameraColorTargetHandle, tempTextures[0]);
            //降采样模糊
            for (int i = 0; i < BlurIterations; i++)
                command.Blit(tempTextures[i], tempTextures[i + 1], material, 0);
            //升采样模糊
            for (int i = 0; i < BlurIterations; i++)
                command.Blit(tempTextures[BlurIterations + i], tempTextures[BlurIterations + i + 1], material, 0);
            //提取处理槽中的画面
            if (string.IsNullOrEmpty(CustomRenderTargetID))
                command.Blit(tempTextures.Last(), renderingData.cameraData.renderer.cameraColorTargetHandle);
            else
            {
                RenderTextureDescriptor textureDescriptor = new(targetDescriptor.width, targetDescriptor.height, targetDescriptor.colorFormat);
                RenderingUtils.ReAllocateIfNeeded(ref customRenderTarget, textureDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: CustomRenderTargetID);
                command.Blit(tempTextures.Last(), customRenderTarget);
                command.SetGlobalTexture(CustomRenderTargetID, customRenderTarget);
            }

            context.ExecuteCommandBuffer(command);
            CommandBufferPool.Release(command);
        }

        //回收临时纹理
        ReleaseTempTextures();
    }

    public override void OnDestroy()
    {
        if (customRenderTarget != null)
            RTHandles.Release(customRenderTarget);
    }
}
