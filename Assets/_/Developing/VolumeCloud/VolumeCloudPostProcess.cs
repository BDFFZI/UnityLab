using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeCloudPostProcess : VolumeComponent, IPostProcessComponent
{
    public FloatParameter intensity = new ClampedFloatParameter(0, 0, 1);
    public Vector3Parameter boundsMin = new Vector3Parameter(Vector3.zero);
    public Vector3Parameter boundsMax = new Vector3Parameter(Vector3.zero);

    public bool IsActive() => intensity.value > 0;
    public bool IsTileCompatible() => false;
}
