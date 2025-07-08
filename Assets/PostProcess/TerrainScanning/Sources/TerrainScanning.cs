using UnityEngine;

public class TerrainScanning : SimplePostProcess
{
    protected override void SetupPass(SimplePostProcessPass pass)
    {
        base.SetupPass(pass);
        pass.Material.SetMatrix("_ClipToWorld", Matrix4x4.Inverse(GL.GetGPUProjectionMatrix(Camera.projectionMatrix, true) * Camera.worldToCameraMatrix));
        pass.Material.SetVector("_CameraPosition", Camera.transform.position);
    }
}
