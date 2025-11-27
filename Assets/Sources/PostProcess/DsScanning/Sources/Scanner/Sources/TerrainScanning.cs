using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TerrainScanning : SimplePostProcess
{
    public override RenderPassEvent RenderQueue => RenderPassEvent.AfterRenderingOpaques;

    [SerializeField] [Range(0, 1)] float progress;

    protected override void SetupPass(SimplePostProcessPass pass)
    {
        base.SetupPass(pass);

        pass.Material.SetFloat("_Progress", progress);
        pass.Material.SetVector("_Origin", transform.position);
    }
}
