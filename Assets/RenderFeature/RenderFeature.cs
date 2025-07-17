using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class RenderFeature<TPass> : MonoBehaviour
    where TPass : RenderPass, new()
{
    public Camera Camera => camera;

    protected virtual void SetupPass(TPass pass) { }

    protected virtual void OnEnable()
    {
        camera = GetComponent<Camera>();
        cameraData = GetComponent<UniversalAdditionalCameraData>();

        postProcessPass = new TPass();
        postProcessPass.Awake();
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    protected virtual void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        postProcessPass.OnDestroy();
        postProcessPass = null;
    }

    new Camera camera;
    UniversalAdditionalCameraData cameraData;
    TPass postProcessPass;

    void OnBeginCameraRendering(ScriptableRenderContext renderContext, Camera camera)
    {
        if (camera != this.camera)
            return;

        SetupPass(postProcessPass);
        cameraData.scriptableRenderer.EnqueuePass(postProcessPass);
    }
}
