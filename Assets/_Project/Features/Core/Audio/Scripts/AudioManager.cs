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

        private SoundId _currentBgmId = SoundId.None;
        private bool _isJinglePlaying = false;
        private SoundId _queuedBgmId = SoundId.None;
        private (SoundId id, float duration) _queuedFadeBgm = (SoundId.None, 0f);

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

        public void PlayBGM(SoundId bgmId)
        {
            if (_isJinglePlaying)
            {
                // BGM再生を予約し、フェードの予約はクリアする
                _queuedBgmId = bgmId;
                _queuedFadeBgm = (SoundId.None, 0f);
                return;
            }

            if (_currentBgmId == bgmId && _bgmSource.isPlaying) return;

            var clip = _registry.GetClip(bgmId);
            if (clip != null)
            {
                // 実行するので予約はすべてクリア
                ClearQueue();
                if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

                _currentBgmId = bgmId;
                _bgmSource.clip = clip;
                _bgmSource.volume = _defaultBgmVolume;
                _bgmSource.Play();
            }
        }

        public void FadeInBGM(SoundId bgmId, float duration)
        {
            if (_isJinglePlaying)
            {
                // フェードインを予約し、通常のBGM再生予約はクリアする
                _queuedFadeBgm = (bgmId, duration);
                _queuedBgmId = SoundId.None;
                return;
            }

            var clip = _registry.GetClip(bgmId);
            if (clip != null)
            {
                // 実行するので予約はすべてクリア
                ClearQueue();
                _currentBgmId = bgmId;
                if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = StartCoroutine(FadeBGM(clip, duration, true));
            }
        }

        public void StopBGM()
        {
            _bgmSource.Stop();
            _bgmSource.clip = null;
            _currentBgmId = SoundId.None;
        }

        public void PlayJingle(SoundId jingleId)
        {
            var clip = _registry.GetClip(jingleId);
            if (clip != null)
            {
                StartCoroutine(JingleCoroutine(clip));
            }
        }

        private IEnumerator JingleCoroutine(AudioClip clip)
        {
            if (_isJinglePlaying) yield break;

            _isJinglePlaying = true;

            if (_bgmSource.isPlaying)
            {
                _bgmSource.volume *= 0.3f;
            }

            _sfxSource.PlayOneShot(clip);

            yield return new WaitForSeconds(clip.length);
            
            _isJinglePlaying = false;

            // 👇 ジングル終了後、予約されている処理を実行
            // フェードインの予約があるか？
            if (_queuedFadeBgm.id != SoundId.None)
            {
                FadeInBGM(_queuedFadeBgm.id, _queuedFadeBgm.duration);
            }
            // 通常再生の予約があるか？
            else if (_queuedBgmId != SoundId.None)
            {
                PlayBGM(_queuedBgmId);
            }
            // 予約がなければ、元のBGMの音量を戻す
            else if (_bgmSource.isPlaying)
            {
                _bgmSource.volume = _defaultBgmVolume;
            }
        }
        
        /// <summary>
        /// 全ての予約をクリアするヘルパーメソッド
        /// </summary>
        public void ClearQueue()
        {
            _queuedBgmId = SoundId.None;
            _queuedFadeBgm = (SoundId.None, 0f);
        }

        /// <summary>
        /// BGMとジングルを停止し、予約された再生キューをすべてクリアします。
        /// ゲームの状態が変わり、予約したオーディオ再生が不要になった場合に使用します。
        /// </summary>
        public void ResetAudio()
        {
            // 実行中のフェードコルーチンを停止
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            // BGMとジングル（SFXソース）を停止
            _bgmSource.Stop();
            _sfxSource.Stop();
            
            _currentBgmId = SoundId.None;
            _isJinglePlaying = false; // ジングル再生状態もリセット

            // 予約キューをクリア
            ClearQueue();
        }

        public void FadeOutBGM(float duration)
        {
            if (!_bgmSource.isPlaying) return;
            _currentBgmId = SoundId.None;
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
                if (_currentBgmId != _registry.GetId(clip))
                {
                    yield break;
                }
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
        /// Plays a one-shot sound effect locally.
        /// Used for UI sounds or effects specific to the local player.
        /// </summary>
        public void PlaySfx(SoundId sfxId)
        {
            var clip = _registry.GetClip(sfxId);
            if (clip != null)
            {
                _sfxSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Plays a one-shot sound effect locally with a randomized pitch.
        /// Ideal for frequent sounds like typing or footsteps to avoid monotony.
        /// </summary>
        public void PlaySfxWithRandomPitch(SoundId sfxId, float minPitch = 0.9f, float maxPitch = 1.1f)
        {
            var clip = _registry.GetClip(sfxId);
            if (clip != null)
            {
                // Set a random pitch for this specific shot
                _sfxSource.pitch = Random.Range(minPitch, maxPitch);
                // Play the sound
                _sfxSource.PlayOneShot(clip);
                // Reset the pitch back to default for the next sound that might not be randomized
                _sfxSource.pitch = 1.0f;
            }
        }

        /// <summary>
        /// Server-side method to request playing a one-shot SFX on all clients.
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

        /// <summary>
        /// Server-side method to request playing a sound on all clients at a specific position with random pitch.
        /// </summary>
        public void PlaySoundAtPointWithRandomPitch(SoundId id, Vector3 position, float minPitch = 0.9f, float maxPitch = 1.1f)
        {
            if (!IsServer) return;
            PlaySoundAtPointWithRandomPitchClientRpc(id, position, minPitch, maxPitch);
        }

        [ClientRpc]
        private void PlaySoundAtPointWithRandomPitchClientRpc(SoundId id, Vector3 position, float minPitch, float maxPitch)
        {
            var clip = _registry.GetClip(id);
            if (clip != null)
            {
                // Create a temporary GameObject to host the AudioSource
                GameObject tempAudioHost = new GameObject("TempAudio");
                tempAudioHost.transform.position = position;
                AudioSource audioSource = tempAudioHost.AddComponent<AudioSource>();

                // Configure the AudioSource
                audioSource.clip = clip;
                audioSource.pitch = Random.Range(minPitch, maxPitch);
                // TODO: Configure other properties like spatialBlend, volume, etc. if needed

                // Play the sound
                audioSource.Play();

                // Destroy the temporary GameObject after the clip has finished playing
                Destroy(tempAudioHost, clip.length);
            }
        }
    }
}
