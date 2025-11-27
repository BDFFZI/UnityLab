using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.VFX;

public class DsScanning : MonoBehaviour
{
    [ContextMenu("Scanning")]
    public void Scanning()
    {
        transform.position = scanningCamera.transform.position;
        landmarkCamera.Render();
        landmarkEffect.Reinit();
        playableDirector.Play();
    }

    [SerializeField] RenderFeatureSystem scannerRenderSystem;
    [SerializeField] Camera landmarkCamera;
    [SerializeField] VisualEffect landmarkEffect;
    [SerializeField] PlayableDirector playableDirector;
    [SerializeField] bool autoPlay;
    [SerializeField] Camera scanningCamera;

    void Start()
    {
        scannerRenderSystem.Camera = scanningCamera;
        scannerRenderSystem.gameObject.SetActive(true);
    }

    void Update()
    {
        if (autoPlay && playableDirector.state != PlayState.Playing)
            Scanning();
    }
}
