using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//https://zhuanlan.zhihu.com/p/143788955
public class TerrainScanningFeature : ScriptableRendererFeature
{
    [SerializeField] Shader shader;

    FullScreenPass fullScreenPass;
    static readonly int width = Shader.PropertyToID("_Width");
    static readonly int distance = Shader.PropertyToID("_Distance");

    public override void Create()
    {
        shader = Shader.Find("Effects/TerrainScanning");
        fullScreenPass = new FullScreenPass("Effects/TerrainScanning");
        fullScreenPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        TerrainScanningPostProcess postProcess = VolumeManager.instance.stack.GetComponent<TerrainScanningPostProcess>();
        if (postProcess.IsActive() == false)
            return;

        if (fullScreenPass.Material == null)
            fullScreenPass.Material = new Material(shader);
        Material material = fullScreenPass.Material;
        material.SetFloat(width, postProcess.width.value);
        material.SetFloat(distance, postProcess.distance.value);

        renderer.EnqueuePass(fullScreenPass);
    }
}
