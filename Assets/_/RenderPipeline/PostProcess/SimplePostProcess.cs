    using UnityEngine;

public class SimplePostProcess : PostProcess<SimplePostProcessPass>
{
    [SerializeField] Material material;
    
    protected override void SetupPass(SimplePostProcessPass pass)
    {
        pass.Material = material;
    }
}
