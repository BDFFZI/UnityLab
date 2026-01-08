using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class GroundCracked : MonoBehaviour
{
    [SerializeField] VisualEffect effect;
    [SerializeField] new Rigidbody rigidbody;
    [SerializeField] new Camera camera;

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            float3 cameraPosition = camera.transform.position;
            cameraPosition.y = 0;
            transform.position = cameraPosition;
            transform.forward = camera.transform.forward;
            rigidbody.velocity = transform.forward * 20;
            effect.Reinit();
        }

        float3 position = transform.position;
        float3 up = transform.up;
        if (Physics.Raycast(position + up, -up, out RaycastHit hit))
            transform.position = hit.point;
    }
}
