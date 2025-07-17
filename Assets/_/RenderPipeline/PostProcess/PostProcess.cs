using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class PostProcess<TPass> : RenderFeature<TPass>
    where TPass : RenderPass, new()
{
    protected override void SetupPass(TPass pass)
    {
        base.SetupPass(pass);
        pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }
}
