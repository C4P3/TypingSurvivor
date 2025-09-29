using UnityEngine;

namespace TypingSurvivor.Features.Core.Audio
{
    /// <summary>
    /// Manages the playback of all audio (BGM and SFX) in the application.
    /// This is a persistent singleton that lives throughout the entire application lifecycle.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioRegistry _registry;

        public void Initialize(AudioRegistry registry)
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

        // TODO: Implement methods for playing BGM, SFX, and handling ClientRpc calls.
    }
}
