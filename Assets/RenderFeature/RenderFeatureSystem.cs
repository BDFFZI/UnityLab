using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(-10)]
public class RenderFeatureSystem : MonoBehaviour
{
    public Camera Camera => camera;

    public void AddRenderPass(int priority, IRenderFeature renderFeature)
    {
        if (renderFeatures.TryGetValue(priority, out List<IRenderFeature> renderPassList) == false)
        {
            renderPassList = new List<IRenderFeature>();
            renderFeatures.Add(priority, renderPassList);
        }

        renderPassList.Add(renderFeature);
    }
    public void RemoveRenderPass(int priority, IRenderFeature renderFeature)
    {
        renderFeatures[priority].Remove(renderFeature);
    }


    new Camera camera;
    UniversalAdditionalCameraData cameraData;
    SortedList<int, List<IRenderFeature>> renderFeatures;

    void OnEnable()
    {
        camera = GetComponent<Camera>();
        cameraData = GetComponent<UniversalAdditionalCameraData>();
        renderFeatures = new SortedList<int, List<IRenderFeature>>();

        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    void OnBeginCameraRendering(ScriptableRenderContext renderContext, Camera camera)
    {
        if (camera != this.camera)
            return;

        foreach (List<IRenderFeature> renderFeatureList in renderFeatures.Values)
        {
            foreach (IRenderFeature renderFeature in renderFeatureList)
            {
                renderFeature.OnRenderPass();
                cameraData.scriptableRenderer.EnqueuePass(renderFeature.RenderPass);
            }
        }
    }
}
