using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class PostProcess<TPass> : MonoBehaviour
    where TPass : PostProcessPass, new()
{
    public TPass PostProcessPass => postProcessPass;

    new Camera camera;
    TPass postProcessPass;

    void Awake()
    {
        camera = GetComponent<Camera>();
    }

    protected virtual void OnEnable()
    {
        postProcessPass = new TPass();
        postProcessPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        postProcessPass.Dispose();
    }

    protected virtual void OnBeginCameraRendering(ScriptableRenderContext arg1, Camera arg2)
    {
        if (camera != arg2)
            return;

        CustomRendererFeature.RenderPasses.Add(postProcessPass);
    }
}
