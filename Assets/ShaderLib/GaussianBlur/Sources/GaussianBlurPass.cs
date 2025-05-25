using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GaussianBlurPass : ScriptableRenderPass
{
    public RenderTexture RenderTarget { get; set; }

    public void Setup(int downSampling, int radius, float variance)
    {
        this.downSampling = downSampling;

        material.SetInt(BlurRadiusID, radius);

        float totalWeight = 0;
        for (int i = -radius; i <= radius; i++)
        {
            float weight = Mathf.Exp(-(i * i) / (2 * variance)) / Mathf.Sqrt(2 * Mathf.PI * variance);
            gaussianDistribution[i + radius] = weight;
            totalWeight += weight;
        }
        for (int i = 0; i < radius * 2 + 1; i++)
            gaussianDistribution[i] /= totalWeight;

        material.SetFloatArray(BlurWeightID, gaussianDistribution);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("GaussianBlur");
        int width = RenderTarget.width / downSampling;
        int height = RenderTarget.height / downSampling;
        cmd.GetTemporaryRT(TempTargetID, width, height, 0, FilterMode.Bilinear);
        cmd.GetTemporaryRT(TempTarget2ID, width, height, 0, FilterMode.Bilinear);

        cmd.Blit(RenderTarget, TempTargetID);
        cmd.Blit(TempTargetID, TempTarget2ID, material, 0);
        cmd.Blit(TempTarget2ID, RenderTarget, material, 1);
        context.ExecuteCommandBuffer(cmd);

        cmd.ReleaseTemporaryRT(TempTargetID);
        cmd.ReleaseTemporaryRT(TempTarget2ID);
        CommandBufferPool.Release(cmd);
    }

    static readonly int TempTargetID = Shader.PropertyToID("_TempTarget");
    static readonly int TempTarget2ID = Shader.PropertyToID("_TempTarget2");
    static readonly int BlurRadiusID = Shader.PropertyToID("_BlurRadius");
    static readonly int BlurWeightID = Shader.PropertyToID("_BlurWeight");
    readonly Material material = new Material(Shader.Find("Unlit/GaussianBlur"));
    readonly float[] gaussianDistribution = new float[7];
    int downSampling = 2;
}
