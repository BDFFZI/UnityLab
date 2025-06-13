using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class BaseProcess : PostProcess
{
    [SerializeField] [Range(0, 2)] float intensity = 1.0f;
    [SerializeField] [Range(0, 2)] float saturate = 1.0f;
    [SerializeField] [Range(0, 2)] float contrast = 1.0f;

    protected override void OnEnable()
    {
        Material = new Material(Shader.Find("Hidden/BaseProcess"));

        base.OnEnable();
    }

    protected override void OnBeginCameraRendering(ScriptableRenderContext arg1, Camera arg2)
    {
        Material.SetFloat("_Intensity", intensity);
        Material.SetFloat("_Saturate", saturate);
        Material.SetFloat("_Contrast", contrast);

        base.OnBeginCameraRendering(arg1, arg2);
    }
}
