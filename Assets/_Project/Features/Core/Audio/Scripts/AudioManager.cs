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
        private AudioSource _bgmSource;
        private float _defaultBgmPitch;

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

            // Create and configure the BGM AudioSource
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _defaultBgmPitch = _bgmSource.pitch;
        }

        /// <summary>
        /// Plays a background music track.
        /// </summary>
        public void PlayBGM(SoundId bgmId)
        {
            var clip = _registry.GetClip(bgmId);
            if (clip != null)
            {
                _bgmSource.clip = clip;
                _bgmSource.Play();
            }
            else
            {
                Debug.LogWarning($"[AudioManager] BGM clip for ID '{bgmId}' not found in registry.");
            }
        }

        /// <summary>
        /// Sets the pitch of the background music.
        /// </summary>
        public void SetBgmPitch(float pitch)
        {
            _bgmSource.pitch = pitch;
        }

        /// <summary>
        /// Resets the background music pitch to its default value.
        /// </summary>
        public void ResetBgmPitch()
        {
            _bgmSource.pitch = _defaultBgmPitch;
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
