using System;
using UnityEngine;

public class Shield : MonoBehaviour
{
    [SerializeField] ParticleSystem hitPoints;
    [SerializeField] new Camera camera;
    [SerializeField] float coolingTime = 0.02f;

    static readonly int HitCount = Shader.PropertyToID("_HitCount");
    static readonly int HitPositions = Shader.PropertyToID("_HitPositions");
    static readonly int HitSizes = Shader.PropertyToID("_HitSizes");

    float timer;
    ParticleSystem.Particle[] hitParticles;
    Vector4[] hitPositions;
    float[] hitSizes;

    void Start()
    {
        hitParticles = new ParticleSystem.Particle[hitPoints.main.maxParticles];
        hitPositions = new Vector4[hitPoints.main.maxParticles];
        hitSizes = new float[hitPoints.main.maxParticles];
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (Input.GetMouseButton(0) && timer > coolingTime)
        {
            timer = 0;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.CompareTag("Shield"))
            {
                ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
                emitParams.position = hit.point;
                hitPoints.Emit(emitParams, 1);
            }
        }

        int particleCount = hitPoints.GetParticles(hitParticles);
        for (int i = 0; i < particleCount; i++)
        {
            hitPositions[i] = hitParticles[i].position;
            hitSizes[i] = hitParticles[i].GetCurrentSize(hitPoints);
        }

        Shader.SetGlobalInt(HitCount, particleCount);
        Shader.SetGlobalVectorArray(HitPositions, hitPositions);
        Shader.SetGlobalFloatArray(HitSizes, hitSizes);
    }
}
