using System;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(ParticleSystem))]
public class AbsoluteField : MonoBehaviour
{
    [SerializeField] int count;

    new ParticleSystem particleSystem;
    ParticleSystem.Particle[] particles;

    void OnEnable()
    {
        particleSystem = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if (particleSystem.particleCount < count)
            particleSystem.Emit(count - particleSystem.particleCount);
        if (particles == null || particles.Length != count)
            particles = new ParticleSystem.Particle[count];

        particleSystem.GetParticles(particles);
        for (int i = 0; i < count; i++)
        {
            particles[i].startSize = (i + 1) / (float)count;
            if (particles[i].remainingLifetime <= Time.deltaTime * 2)
                particles[i].remainingLifetime = particles[i].startLifetime;
        }
        particleSystem.SetParticles(particles, count);
    }
}
