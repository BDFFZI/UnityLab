using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public abstract class PostProcessPass : ScriptableRenderPass, IDisposable
{
    public virtual void Dispose() { }
}
