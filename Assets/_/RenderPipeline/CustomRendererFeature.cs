using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

public class CustomRendererFeature : ScriptableRendererFeature
{
    public static SortedList<int, ScriptableRenderPass> RenderPasses { get; } = new();

    public override void Create() { }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        foreach (ScriptableRenderPass renderPass in RenderPasses.Values)
        {
            if (renderPass != null)
                renderer.EnqueuePass(renderPass);
        }
        RenderPasses.Clear();
    }
}
