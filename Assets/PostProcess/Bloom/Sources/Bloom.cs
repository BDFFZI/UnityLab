using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DefaultExecutionOrder(10)]
public class Bloom : PostProcess<BloomPass>
{
    [SerializeField] float threshold = 1;
    [SerializeField] float intensity = 1;
    [SerializeField] [Range(1, 4)] int downsample = 2;
    [SerializeField] [Range(0, 8)] int blurIterations = 4;
    [SerializeField] int blurRadius = 3;

    protected override void SetupPass(BloomPass pass)
    {
        pass.Threshold = threshold;
        pass.Intensity = intensity;
        pass.Downsample = downsample;
        pass.BlurIterations = blurIterations;
        pass.BlurRadius = blurRadius;
    }
}

public class BloomPass : PostProcessPass
{
    public float Threshold { get; set; }
    public float Intensity { get; set; }
    public int Downsample { get; set; }
    public int BlurRadius { get; set; }
    public int BlurIterations { get; set; }

    Material material = new Material(Shader.Find("Hidden/Bloom"));
    List<RenderTexture> tempTextures = new List<RenderTexture>();


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
        material.SetFloat("_Threshold", Threshold);
        material.SetFloat("_Intensity", Intensity);
        material.SetInt("_BlurRadius", BlurRadius / Downsample);

        RenderTextureDescriptor renderTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;

        CreateTempTextures(renderTextureDescriptor);
        RenderTexture source = RenderTexture.GetTemporary(renderTextureDescriptor.width, renderTextureDescriptor.height, 0, renderTextureDescriptor.colorFormat);
        {
            CommandBuffer command = CommandBufferPool.Get("Bloom");

            //找出发光区域
            command.Blit(renderingData.cameraData.renderer.cameraColorTargetHandle, tempTextures[0], material, 0);
            //降采样模糊
            for (int i = 0; i < BlurIterations; i++)
                command.Blit(tempTextures[i], tempTextures[i + 1], material, 1);
            //升采样模糊增量
            for (int i = 0; i < BlurIterations; i++)
            {
                command.SetGlobalTexture("_BloomTex", tempTextures[BlurIterations - i - 1]);
                command.Blit(tempTextures[BlurIterations + i], tempTextures[BlurIterations + i + 1], material, 2);
            }
            //输出合并
            command.SetGlobalTexture("_BloomTex", tempTextures.Last());
            command.Blit(renderingData.cameraData.renderer.cameraColorTargetHandle, source); //复制源图像
            command.Blit(source, renderingData.cameraData.renderer.cameraColorTargetHandle, material, 3);

            context.ExecuteCommandBuffer(command);
            CommandBufferPool.Release(command);
        }
        RenderTexture.ReleaseTemporary(source);
        ReleaseTempTextures();
    }
}
