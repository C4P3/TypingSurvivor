using Unity.Netcode;
using UnityEngine;

namespace TypingSurvivor.Features.Core.Audio
{
    /// <summary>
    /// Manages the playback of all audio (BGM and SFX) in the application.
    /// This is a persistent singleton that lives throughout the entire application lifecycle.
    /// </summary>
    public class AudioManager : NetworkBehaviour
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

        /// <summary>
        /// Server-side method to request playing a sound on all clients at a specific position.
        /// </summary>
        public void PlaySoundAtPoint(SoundId id, Vector3 position)
        {
            if (!IsServer) return;
            PlaySoundAtPointClientRpc(id, position);
        }

        [ClientRpc]
        private void PlaySoundAtPointClientRpc(SoundId id, Vector3 position)
        {
            var clip = _registry.GetClip(id);
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, position);
            }
        }
    }
}
