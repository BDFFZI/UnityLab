using UnityEngine;
using UnityEngine.Rendering.Universal;

public interface IRenderFeature
{
    RenderPass RenderPass { get; }
    void OnRenderPass();
}

[RequireComponent(typeof(RenderFeatureSystem))]
[ExecuteAlways]
public class RenderFeature<TPass> : MonoBehaviour, IRenderFeature
    where TPass : RenderPass, new()
{
    public virtual RenderPassEvent RenderQueue => RenderPassEvent.AfterRenderingOpaques;
    public virtual int RenderOrder => 0;

    public RenderPass RenderPass => renderPass;
    public Camera Camera => renderFeatureSystem.Camera;

    protected virtual void OnEnable()
    {
        renderFeatureSystem = GetComponent<RenderFeatureSystem>();

        renderPass = new TPass();
        renderPass.renderPassEvent = RenderQueue;
        renderPass.Awake();
        renderFeatureSystem.AddRenderPass(RenderOrder, this);
    }

    protected virtual void OnDisable()
    {
        renderFeatureSystem.RemoveRenderPass(RenderOrder, this);
        renderPass.OnDestroy();
        renderPass = null;
    }

    protected virtual void SetupPass(TPass pass)
    {
        //Null
    }

    RenderFeatureSystem renderFeatureSystem;
    TPass renderPass;

    public void OnRenderPass()
    {
        SetupPass(renderPass);
    }
}
