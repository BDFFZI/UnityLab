using UnityEngine;
using UnityEngine.Rendering;

public class SimplePostProcess : PostProcess<SimplePostProcessPass>
{
    [SerializeField] Material material;

    protected override void OnBeginCameraRendering(ScriptableRenderContext arg1, Camera arg2)
    {
        PostProcessPass.Material = material;
        base.OnBeginCameraRendering(arg1, arg2);
    }
}
