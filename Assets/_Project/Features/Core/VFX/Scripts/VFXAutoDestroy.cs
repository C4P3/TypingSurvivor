using UnityEngine;

namespace TypingSurvivor.Features.Core.VFX
{
    /// <summary>
    /// Destroys the GameObject this component is attached to after the main ParticleSystem has finished playing.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class VFXAutoDestroy : MonoBehaviour
    {
        private ParticleSystem _particleSystem;

        private void Start()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            // Destroy the GameObject after the duration of the particle system.
            // The 'main.duration' is the length of the particle emission, and 'main.startLifetime' is how long particles live.
            // We take the maximum of these to ensure the effect fully completes.
            float lifetime = _particleSystem.main.duration + _particleSystem.main.startLifetime.constantMax;
            Destroy(gameObject, lifetime);
        }
    }
}
