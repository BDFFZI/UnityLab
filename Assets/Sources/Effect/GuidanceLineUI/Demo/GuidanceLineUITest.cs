using UnityEngine;

public class GuidanceLineUITest : MonoBehaviour
{
    [SerializeField] GuidanceLineUI guidanceLineUI;

    void Update()
    {
        if (Input.GetMouseButton(0))
            guidanceLineUI.PlayCharge(Mathf.Clamp01(guidanceLineUI.ChargeProgress + Time.deltaTime * 0.4f));
        else if (Input.GetMouseButtonUp(0))
            guidanceLineUI.StopCharge();
    }
}