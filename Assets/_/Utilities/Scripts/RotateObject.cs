using UnityEngine;

public class RotateObject : MonoBehaviour
{
    [SerializeField] float speed = 30;

    void Update()
    {
        transform.Rotate(Vector3.up, Time.deltaTime * speed);
    }
}
