using System.Collections;
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
        private AudioSource _sfxSource; // For one-shot sounds like jingles
        private float _defaultBgmVolume;
        private Coroutine _fadeCoroutine;

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
            _defaultBgmVolume = _bgmSource.volume;

            // Create and configure the SFX AudioSource
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
        }

        /// <summary>
        /// Plays a background music track, avoiding re-playing the same track.
        /// </summary>
        public void PlayBGM(SoundId bgmId)
        {
            var clip = _registry.GetClip(bgmId);
            if (clip != null)
            {
                if (_bgmSource.clip == clip && _bgmSource.isPlaying)
                {
                    return; // Already playing this clip
                }
                _bgmSource.clip = clip;
                _bgmSource.volume = _defaultBgmVolume;
                _bgmSource.Play();
            }
        }

        public void StopBGM()
        {
            _bgmSource.Stop();
            _bgmSource.clip = null;
        }

        public void PlayJingle(SoundId jingleId, System.Action onComplete = null)
        {
            var clip = _registry.GetClip(jingleId);
            if (clip != null)
            {
                StartCoroutine(JingleCoroutine(clip, onComplete));
            }
        }
        private IEnumerator JingleCoroutine(AudioClip clip, System.Action onComplete)
        {
            _bgmSource.volume *= 0.3f; // Lower BGM volume
            _sfxSource.PlayOneShot(clip);
            yield return new WaitForSeconds(clip.length);
            _bgmSource.volume = _defaultBgmVolume; // Restore BGM volume
            onComplete?.Invoke();
        }

        public void FadeInBGM(SoundId bgmId, float duration)
        {
            var clip = _registry.GetClip(bgmId);
            if (clip != null)
            {
                if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = StartCoroutine(FadeBGM(clip, duration, true));
            }
        }

        public void FadeOutBGM(float duration)
        {
            if (!_bgmSource.isPlaying) return;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeBGM(null, duration, false));
        }

        private IEnumerator FadeBGM(AudioClip clip, float duration, bool fadeIn)
        {
            if (fadeIn)
            {
                _bgmSource.clip = clip;
                _bgmSource.volume = 0;
                _bgmSource.Play();
            }

            float startVolume = _bgmSource.volume;
            float targetVolume = fadeIn ? _defaultBgmVolume : 0;
            float time = 0;

            while (time < duration)
            {
                time += Time.deltaTime;
                _bgmSource.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
                yield return null;
            }

            _bgmSource.volume = targetVolume;
            if (!fadeIn)
            {
                _bgmSource.Stop();
                _bgmSource.clip = null;
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
            _bgmSource.pitch = 1.0f;
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
