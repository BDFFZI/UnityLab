using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

public class CustomRendererFeature : ScriptableRendererFeature
{
    public static List<ScriptableRenderPass> RenderPasses { get; } = new List<ScriptableRenderPass>();

    public override void Create() { }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        foreach (ScriptableRenderPass renderPass in RenderPasses)
        {
            if (renderPass != null)
                renderer.EnqueuePass(renderPass);
        }
        RenderPasses.Clear();
    }
}
