using UnityEngine;
using UnityEngine.UI;

public class GuidanceLineUI : MonoBehaviour
{
    public Transform Target { get => guidanceLine.Target; set => guidanceLine.Target = value; }
    public float ChargeProgress => image.fillAmount;
    public void PlayCharge(float progress)
    {
        guidanceLine.IsBursting = true;
        image.fillAmount = progress;
        if (chargeAudio.isPlaying == false)
            chargeAudio.Play();
    }
    public void StopCharge()
    {
        guidanceLine.IsBursting = false;
        chargeAudio.Stop();
        image.fillAmount = 0;
    }

    [SerializeField] GuidanceLine guidanceLine;
    [SerializeField] Image image;
    [SerializeField] AudioSource chargeAudio;
}
