using System;
using UnityEngine.Rendering.Universal;

public abstract class RenderPass : ScriptableRenderPass
{
    public virtual void Awake() { }
    public virtual void OnDestroy() { }
}
