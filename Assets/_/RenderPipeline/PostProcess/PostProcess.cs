using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class PostProcess<TPass> : MonoBehaviour
    where TPass : PostProcessPass, new()
{
    protected virtual void SetupPass(TPass pass) { }

    new Camera camera;
    UniversalAdditionalCameraData cameraData;
    TPass postProcessPass;

    protected virtual void OnEnable()
    {
        camera = GetComponent<Camera>();
        cameraData = GetComponent<UniversalAdditionalCameraData>();

        postProcessPass = new TPass();
        postProcessPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }


    protected virtual void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        postProcessPass.Dispose();
    }


    void OnBeginCameraRendering(ScriptableRenderContext renderContext, Camera camera)
    {
        if (camera != this.camera)
            return;

        SetupPass(postProcessPass);
        cameraData.scriptableRenderer.EnqueuePass(postProcessPass);
    }
}
