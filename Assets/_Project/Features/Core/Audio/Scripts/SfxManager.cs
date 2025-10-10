using TypingSurvivor.Features.Core.Audio.Data;
using TypingSurvivor.Features.Core.Settings;
using Unity.Netcode;
using UnityEngine;

namespace TypingSurvivor.Features.Core.Audio
{
    /// <summary>
    /// Manages the playback of all sound effects (SFX).
    /// This is a persistent singleton that plays sounds based on SoundEffectData assets.
    /// </summary>
    public class SfxManager : NetworkBehaviour
    {
        public static SfxManager Instance { get; private set; }

        private AudioRegistry _registry;
        private AudioSource _sfxSource; // For local, non-positional SFX like UI sounds
        private float _sfxVolumeMultiplier = 1.0f;

        public void Initialize(AudioRegistry registry)
        {
            _registry = registry;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
        }

        private void Start()
        {
            if (SettingsManager.Instance == null) return;
            
            _sfxVolumeMultiplier = SettingsManager.Instance.Settings.SfxVolume;
            SettingsManager.Instance.OnSfxVolumeChanged += HandleSfxVolumeChanged;
        }

        override public void OnDestroy()
        {
            base.OnDestroy();
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OnSfxVolumeChanged -= HandleSfxVolumeChanged;
            }
        }

        private void HandleSfxVolumeChanged(float volume)
        {
            _sfxVolumeMultiplier = volume;
        }

        /// <summary>
        /// Plays a one-shot sound effect locally (e.g., for UI).
        /// </summary>
        public void PlaySfx(SoundId sfxId)
        {
            var sfxData = _registry.GetSfxData(sfxId);
            if (sfxData == null || sfxData.Clips.Count == 0) return;

            AudioClip clip = sfxData.Clips[Random.Range(0, sfxData.Clips.Count)];
            _sfxSource.pitch = sfxData.RandomizePitch ? Random.Range(sfxData.MinPitch, sfxData.MaxPitch) : 1.0f;
            _sfxSource.PlayOneShot(clip, sfxData.Volume * _sfxVolumeMultiplier);
            _sfxSource.pitch = 1.0f; // Reset pitch immediately
        }

        /// <summary>
        /// Server-side method to request playing a non-positional SFX on all clients.
        /// </summary>
        public void PlaySfxOnAllClients(SoundId sfxId)
        {
            if (!IsServer) return;
            PlaySfxClientRpc(sfxId);
        }

        [ClientRpc]
        private void PlaySfxClientRpc(SoundId sfxId)
        {
            PlaySfx(sfxId);
        }

        /// <summary>
        /// Server-side method to request playing a positional SFX on all clients.
        /// </summary>
        public void PlaySfxAtPoint(SoundId sfxId, Vector3 position)
        {
            if (!IsServer) return;
            PlaySfxAtPointClientRpc(sfxId, position);
        }

        [ClientRpc]
        private void PlaySfxAtPointClientRpc(SoundId sfxId, Vector3 position)
        {
            var sfxData = _registry.GetSfxData(sfxId);
            if (sfxData == null || sfxData.Clips.Count == 0) return;

            AudioClip clip = sfxData.Clips[Random.Range(0, sfxData.Clips.Count)];
            float pitch = sfxData.RandomizePitch ? Random.Range(sfxData.MinPitch, sfxData.MaxPitch) : 1.0f;

            // Create a temporary GameObject to play the positional sound
            GameObject tempAudioHost = new GameObject($"SFX_{sfxId}");
            tempAudioHost.transform.position = position;
            AudioSource audioSource = tempAudioHost.AddComponent<AudioSource>();

            audioSource.clip = clip;
            audioSource.volume = sfxData.Volume * _sfxVolumeMultiplier;
            audioSource.pitch = pitch;
            audioSource.spatialBlend = 1.0f; // Make it a 3D sound

            audioSource.Play();

            Destroy(tempAudioHost, clip.length * pitch);
        }
    }
}
