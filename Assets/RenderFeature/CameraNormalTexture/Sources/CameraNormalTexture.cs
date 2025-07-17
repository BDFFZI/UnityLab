using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraNormalTexture : RenderFeature<CameraNormalTexturePass>
{
    [SerializeField] bool preview;

    protected override void SetupPass(CameraNormalTexturePass pass)
    {
        base.SetupPass(pass);
        pass.Preview = preview;
    }
}

public class CameraNormalTexturePass : RenderPass
{
    public bool Preview { get; set; }

    static readonly int CameraNormalTextureID = Shader.PropertyToID("_CameraNormalTexture");

    RTHandle renderTarget;
    Material material;
    List<ShaderTagId> shaderTags;

    public override void Awake()
    {
        base.Awake();

        material = new(Shader.Find("RenderFeature/CameraNormalTexture"));
        shaderTags = new List<ShaderTagId>() {
            new ShaderTagId("DepthOnly"),
        };
    }

    public override void OnDestroy()
    {
        RTHandles.Release(renderTarget);

        base.OnDestroy();
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        base.OnCameraSetup(cmd, ref renderingData);

        RenderTextureDescriptor renderTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        renderTargetDescriptor.colorFormat = RenderTextureFormat.ARGB32;
        renderTargetDescriptor.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref renderTarget, renderTargetDescriptor, name: "_CameraNormalTexture");

        ConfigureTarget(renderTarget, renderingData.cameraData.renderer.cameraDepthTargetHandle);
        ConfigureClear(ClearFlag.All, Color.black);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        //渲染
        {
            DrawingSettings drawingSettings = CreateDrawingSettings(shaderTags, ref renderingData, SortingCriteria.CommonOpaque);
            drawingSettings.overrideMaterial = material;

            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
        }

        CommandBuffer cmd = CommandBufferPool.Get("CameraNormalTexture");
        cmd.SetGlobalTexture(CameraNormalTextureID, renderTarget);
        if (Preview) //可视化到屏幕
            cmd.Blit(renderTarget, renderingData.cameraData.renderer.cameraColorTargetHandle);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
