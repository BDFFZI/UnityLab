using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class ColorAdjustment : PostProcess<SimplePostProcessPass>
{
    [SerializeField] [Range(0, 2)] float intensity = 1.0f;
    [SerializeField] [Range(0, 2)] float saturate = 1.0f;
    [SerializeField] [Range(0, 2)] float contrast = 1.0f;

    Material material;

    protected override void OnEnable()
    {
        base.OnEnable();

        material = new Material(Shader.Find("Hidden/ColorAdjustment"));
        PostProcessPass.Material = material;
    }

    protected override void OnBeginCameraRendering(ScriptableRenderContext arg1, Camera arg2)
    {
        material.SetFloat("_Intensity", intensity);
        material.SetFloat("_Saturate", saturate);
        material.SetFloat("_Contrast", contrast);

        base.OnBeginCameraRendering(arg1, arg2);
    }
}
