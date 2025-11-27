using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ScreenScanning : PostProcessFeature<ScreenScanningPass>
{
    public override RenderPassEvent RenderQueue => RenderPassEvent.BeforeRenderingPostProcessing;
}

public class ScreenScanningPass : PostProcessPass
{
    public override Material Material { get; } = new Material(Shader.Find("PostProcess/ScreenScanning"));
}
