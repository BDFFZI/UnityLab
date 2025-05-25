using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//https://zhuanlan.zhihu.com/p/248406797
public class VolumeCloudFeature : ScriptableRendererFeature
{
    [SerializeField] Shader shader;

    static readonly int boundsMin = Shader.PropertyToID("_BoundsMin");
    static readonly int boundsMax = Shader.PropertyToID("_BoundsMax");
    static readonly int depthTex = Shader.PropertyToID("_DepthTex");

    FullScreenPass fullScreenPass;

    public override void Create()
    {
        shader = Shader.Find("Unlit/VolumeCloud");
        fullScreenPass = new FullScreenPass("VolumeCloud");
        fullScreenPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        //仅启用后处理时可用
        VolumeCloudPostProcess volumeCloudPostProcess = VolumeManager.instance.stack.GetComponent<VolumeCloudPostProcess>();
        if (volumeCloudPostProcess.IsActive() == false)
            return;

        //获取材质球
        if (fullScreenPass.Material == null)
            fullScreenPass.Material = new Material(shader);
        Material material = fullScreenPass.Material;

        //配置参数
        material.SetVector(boundsMin, volumeCloudPostProcess.boundsMin.value);
        material.SetVector(boundsMax, volumeCloudPostProcess.boundsMax.value);

        //渲染
        renderer.EnqueuePass(fullScreenPass);
    }
}
