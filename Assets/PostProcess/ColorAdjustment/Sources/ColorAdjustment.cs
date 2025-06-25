using UnityEngine;

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
    }

    protected override void SetupPass(SimplePostProcessPass pass)
    {
        material.SetFloat("_Intensity", intensity);
        material.SetFloat("_Saturate", saturate);
        material.SetFloat("_Contrast", contrast);
        pass.Material = material;
    }
}
