    using UnityEngine;

public class SimplePostProcess : PostProcess<SimplePostProcessPass>
{
    [SerializeField] Material material;
    
    protected override void SetupPass(SimplePostProcessPass pass)
    {
        base.SetupPass(pass);
        pass.Material = material;
    }
}
