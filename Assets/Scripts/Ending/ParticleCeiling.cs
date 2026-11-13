using UnityEngine;

// Keeps a particle system's particles under a height: they fade out as they get close to it and are gone before
// they cross it (fog that covers the platform but never rises over the floor the player walks on).
[RequireComponent(typeof(ParticleSystem))]
public class ParticleCeiling : MonoBehaviour
{
    [Tooltip("World height the particles never go past (their top edge), e.g. the platform's walking surface.")]
    [SerializeField] private float ceiling;
    [Tooltip("Over how many units under the ceiling they fade out.")]
    [SerializeField, Min(0.01f)] private float fadeDistance = 1.5f;

    private ParticleSystem system;
    private ParticleSystem.Particle[] particles;

    private void Awake()
    {
        system = GetComponent<ParticleSystem>();
    }

    private void LateUpdate()
    {
        int max = system.main.maxParticles;
        if (particles == null || particles.Length < max)
            particles = new ParticleSystem.Particle[max];

        int count = system.GetParticles(particles);
        for (int i = 0; i < count; i++)
        {
            ParticleSystem.Particle p = particles[i];
            float top = p.position.y + p.GetCurrentSize(system) * 0.5f;
            float room = ceiling - top;
            if (room <= 0f)
            {
                p.remainingLifetime = 0f;
            }
            else
            {
                Color32 c = p.startColor;
                c.a = (byte)(255f * Mathf.Clamp01(room / fadeDistance));
                p.startColor = c;
            }
            particles[i] = p;
        }
        system.SetParticles(particles, count);
    }
}
