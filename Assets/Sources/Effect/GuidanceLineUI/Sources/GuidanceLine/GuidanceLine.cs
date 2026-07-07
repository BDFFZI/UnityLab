using System;
using System.Linq;
using UnityEngine;

public class GuidanceLine : MonoBehaviour
{
    public bool IsBursting { get => isBursting; set => isBursting = value; }
    public Transform Target { get => target; set => target = value; }

    [SerializeField] Transform destination;
    [SerializeField] Transform origin;
    [SerializeField] Animator animator;
    [SerializeField] Transform target;
    [SerializeField] bool isBursting;
    [SerializeField] Renderer breathingLight;
    [SerializeField] float timeSpeed = 1;

    Material[] materials;
    float showingRate;
    float burstingRate;
    float showingVelocity;
    float burstingVelocity;
    float time;

    void Awake()
    {
        materials = GetComponentsInChildren<Renderer>().Select(renderer1 => renderer1.material).ToArray();
    }

    void LateUpdate()
    {
        if (target != null)
        {
            destination.position = target.position;
            breathingLight.material.SetVector("_Direction", Vector3.Normalize(destination.position - origin.position));
        }
        else
        {
            destination.position = origin.position;
        }

        showingRate = Mathf.SmoothDamp(showingRate, target != null ? 1 : 0, ref showingVelocity, 0.5f);
        burstingRate = Mathf.SmoothDamp(burstingRate, isBursting ? 1 : 0, ref burstingVelocity, 0.3f);
        animator.Play("Show", 0, showingRate);
        animator.Play("Burst", 1, burstingRate);

        time += Time.deltaTime * timeSpeed;
        foreach (Material material in materials)
            material.SetFloat("_CustomTime", time);
    }
}
