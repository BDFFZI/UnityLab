using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ImageProcess : MonoBehaviour
{
    [SerializeField] RenderTexture renderTexture;
    [SerializeField] [Range(1, 8)] int blurDownSampling = 2;
    [SerializeField] [Range(0, 3)] int blurRadius = 2;
    [SerializeField] [Min(0.001f)] float blurVariance = 1;

    GaussianBlurPass gaussianBlurPass;

    void Awake()
    {
        gaussianBlurPass = new GaussianBlurPass();
        gaussianBlurPass.RenderTarget = renderTexture;
        gaussianBlurPass.renderPassEvent = RenderPassEvent.AfterRendering;
    }

    void Update()
    {
        gaussianBlurPass.Setup(blurDownSampling, blurRadius, blurVariance);
        CustomPassFeature.RenderPasses.Add(gaussianBlurPass);
    }
}
