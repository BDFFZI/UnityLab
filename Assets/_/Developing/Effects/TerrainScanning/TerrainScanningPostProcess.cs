using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class TerrainScanningPostProcess : VolumeComponent, IPostProcessComponent
{
    public FloatParameter intensity = new ClampedFloatParameter(0, 0, 1);
    public FloatParameter width = new MinFloatParameter(0, 0.01f);
    public FloatParameter distance = new MinFloatParameter(0, 0);

    public bool IsActive() => intensity.value != 0;
    public bool IsTileCompatible() => false;
}
