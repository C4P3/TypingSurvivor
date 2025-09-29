using UnityEngine;

namespace TypingSurvivor.Features.Core.VFX
{
    /// <summary>
    /// Manages the instantiation and playback of all visual effects (VFX) in the application.
    /// This is a persistent singleton that lives throughout the entire application lifecycle.
    /// </summary>
    public class EffectManager : MonoBehaviour
    {
        public static EffectManager Instance { get; private set; }

        private VFXRegistry _registry;

        public void Initialize(VFXRegistry registry)
        {
            _registry = registry;
            _registry.Initialize();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        // TODO: Implement methods for playing VFX and handling ClientRpc calls.
    }
}
