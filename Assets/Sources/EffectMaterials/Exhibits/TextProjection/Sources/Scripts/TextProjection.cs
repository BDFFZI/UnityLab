using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class TextProjection : MonoBehaviour
{
    [Button]
    public Tween SetText(string text)
    {
        textMesh.SetText("");
        return textMesh.DOText(text, textShowDuration).SetEase(Ease.Linear);
    }

    [SerializeField] TextMeshPro textMesh;
    [SerializeField] float textShowDuration = 1;
    [SerializeField] Transform canvasTransform;
    [SerializeField] Transform canvasForward;
    [SerializeField] float maxRotationAngle = 10;

    void Awake()
    {
        textMesh.SetText("");
    }

    void Update()
    {
        if (canvasForward != null)
        {
            float3 newForward = canvasForward.position - canvasTransform.position;
            float3 oldForward = transform.forward;
            float maxRotationRad = math.radians(maxRotationAngle);
            canvasTransform.forward = Vector3.RotateTowards(oldForward, newForward, maxRotationRad, maxRotationRad);
        }
    }
}
