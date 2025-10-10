using System.Collections;
using System.Collections.Generic;
using TypingSurvivor.Features.Core.Audio.Data;
using TypingSurvivor.Features.Core.Settings;
using Unity.Netcode;
using UnityEngine;

namespace TypingSurvivor.Features.Core.Audio
{
    /// <summary>
    /// Manages the playback of all background music (BGM).
    /// This is a persistent singleton that uses two AudioSources (turntables) for seamless crossfading
    /// and a stack-based system for managing temporary BGM tracks.
    /// </summary>
    public class MusicManager : NetworkBehaviour
    {
        public static MusicManager Instance { get; private set; }

        private AudioSource _turntableA;
        private AudioSource _turntableB;
        private AudioSource _activeTurntable;
        
        private AudioRegistry _registry;
        private float _bgmVolumeMultiplier = 1.0f;

        private readonly Stack<MusicData> _musicStack = new();
        private Coroutine _activeTransitionCoroutine;

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

            _turntableA = gameObject.AddComponent<AudioSource>();
            _turntableB = gameObject.AddComponent<AudioSource>();
            _activeTurntable = _turntableA;
        }

        private void Start()
        {
            if (SettingsManager.Instance == null) return;
            
            _bgmVolumeMultiplier = SettingsManager.Instance.Settings.BgmVolume;
            SettingsManager.Instance.OnBgmVolumeChanged += HandleBgmVolumeChanged;
        }

        override public void OnDestroy()
        {
            base.OnDestroy();
            StopAndClear();

            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OnBgmVolumeChanged -= HandleBgmVolumeChanged;
            }
        }

        private void HandleBgmVolumeChanged(float volume)
        {
            _bgmVolumeMultiplier = volume;
            if (_activeTurntable != null && _activeTurntable.isPlaying && _musicStack.Count > 0)
            {
                // Update the volume of the currently playing music
                var currentMusic = _musicStack.Peek();
                _activeTurntable.volume = currentMusic.Volume * _bgmVolumeMultiplier;
            }
        }

        // --- Public API ---

        /// <summary>
        /// Master command. Interrupts everything and plays the specified music.
        /// Clears the entire music stack.
        /// </summary>
        public void Play(MusicData music, float fadeDuration = 1.0f)
        {
            StopAndClear();
            _musicStack.Push(music);
            _activeTransitionCoroutine = StartCoroutine(CrossfadeCoroutine(music, fadeDuration));
        }

        /// <summary>
        /// Pushes a temporary music track onto the stack and plays it.
        /// </summary>
        public void Push(MusicData temporaryMusic, float fadeDuration = 1.0f)
        {
            if (_activeTransitionCoroutine != null) StopCoroutine(_activeTransitionCoroutine);
            _musicStack.Push(temporaryMusic);
            _activeTransitionCoroutine = StartCoroutine(CrossfadeCoroutine(temporaryMusic, fadeDuration));
        }

        /// <summary>
        /// Stops the current music and returns to the previous one on the stack.
        /// </summary>
        public void Pop(float fadeDuration = 1.0f)
        {
            if (_musicStack.Count <= 1) return; // Cannot pop the base track

            if (_activeTransitionCoroutine != null) StopCoroutine(_activeTransitionCoroutine);
            _musicStack.Pop();
            _activeTransitionCoroutine = StartCoroutine(CrossfadeCoroutine(_musicStack.Peek(), fadeDuration));
        }

        /// <summary>
        /// Plays a non-looping jingle, and then transitions to the next music track.
        /// </summary>
        public void PlayJingleThen(MusicData jingle, MusicData nextMusic, float nextMusicFadeInDuration = 1.0f, float overlapDuration = 0f)
        {
            StopAndClear();
            _musicStack.Push(nextMusic); // The next music is the new base
            _activeTransitionCoroutine = StartCoroutine(JingleThenCoroutine(jingle, nextMusic, nextMusicFadeInDuration, overlapDuration));
        }

        /// <summary>
        /// Stops all music playback with a fade-out and clears the stack.
        /// </summary>
        public void Stop(float fadeDuration = 1.0f)
        {
            StopAndClear();
            _activeTransitionCoroutine = StartCoroutine(FadeOutCoroutine(fadeDuration));
        }

        private void StopAndClear()
        {
            if (_activeTransitionCoroutine != null)
            {
                StopCoroutine(_activeTransitionCoroutine);
                _activeTransitionCoroutine = null;
            }
            // Stop AudioSources immediately as a fallback
            if (_turntableA.isPlaying) _turntableA.Stop();
            if (_turntableB.isPlaying) _turntableB.Stop();
            
            _musicStack.Clear();
        }

        // --- Coroutine Wrappers (Managing _activeTransitionCoroutine) ---

        private IEnumerator CrossfadeCoroutine(MusicData music, float duration)
        {
            yield return StartCoroutine(CrossfadeLogic(music, duration));
            _activeTransitionCoroutine = null;
        }

        private IEnumerator FadeOutCoroutine(float duration)
        {
            yield return StartCoroutine(FadeOutLogic(duration));
            _activeTransitionCoroutine = null;
        }
        
        private IEnumerator JingleThenCoroutine(MusicData jingle, MusicData nextMusic, float nextMusicFadeInDuration, float overlapDuration)
        {
            // Fade out current music (if any) using the logic part
            if (_activeTurntable.isPlaying)
            {
                yield return StartCoroutine(FadeOutLogic(0.5f));
            }

            // Play jingle
            _activeTurntable.clip = jingle.Clip;
            _activeTurntable.volume = jingle.Volume * _bgmVolumeMultiplier;
            _activeTurntable.loop = false;
            _activeTurntable.Play();

            // Wait for the jingle to nearly finish
            float jingleDuration = jingle.Clip.length;
            float waitTime = Mathf.Max(0f, jingleDuration - overlapDuration);
            yield return new WaitForSeconds(waitTime);

            // Start crossfade to the next music using the logic part
            yield return StartCoroutine(CrossfadeLogic(nextMusic, nextMusicFadeInDuration));
            
            // This parent coroutine is now finished, so it can clear the state.
            _activeTransitionCoroutine = null;
        }

        // --- Coroutine Logic (Actual Implementation) ---

        private IEnumerator CrossfadeLogic(MusicData music, float duration)
        {
            AudioSource newTurntable = (_activeTurntable == _turntableA) ? _turntableB : _turntableA;
            AudioSource oldTurntable = _activeTurntable;

            // Setup new turntable
            newTurntable.clip = music.Clip;
            newTurntable.volume = 0;
            newTurntable.loop = music.Loop;
            newTurntable.Play();

            _activeTurntable = newTurntable;

            // Fade logic
            float timer = 0f;
            float startOldVolume = oldTurntable.volume; // Store the starting volume for a linear fade
            float targetNewVolume = music.Volume * _bgmVolumeMultiplier;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / duration);
                
                newTurntable.volume = Mathf.Lerp(0, targetNewVolume, progress);
                oldTurntable.volume = Mathf.Lerp(startOldVolume, 0, progress); // Use stored start volume
                yield return null;
            }

            newTurntable.volume = targetNewVolume;
            oldTurntable.Stop();
            oldTurntable.clip = null;
        }
        
        private IEnumerator FadeOutLogic(float duration)
        {
            if (!_activeTurntable.isPlaying) yield break;

            float startVolume = _activeTurntable.volume;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                _activeTurntable.volume = Mathf.Lerp(startVolume, 0, Mathf.Clamp01(timer / duration));
                yield return null;
            }

            _activeTurntable.volume = 0;
            _activeTurntable.Stop();
            _activeTurntable.clip = null;
        }

        public void SetPitch(float pitch)
        {
            _turntableA.pitch = pitch;
            _turntableB.pitch = pitch;
        }

        public void ResetPitch()
        {
            SetPitch(1.0f);
        }

        public void Play(SoundId musicId, float fadeDuration = 1.0f)
        {
            var music = _registry?.GetMusicData(musicId);
            if (music != null) Play(music, fadeDuration);
        }
        
        public void Push(SoundId temporaryMusicId, float fadeDuration = 1.0f)
        {
            var music = _registry?.GetMusicData(temporaryMusicId);
            if (music != null) Push(music, fadeDuration);
        }
        
        public void PlayJingleThen(SoundId jingleId, SoundId nextMusicId, float nextMusicFadeInDuration = 1.0f, float overlapDuration = 0f)
        {
            var jingle = _registry?.GetMusicData(jingleId);
            var nextMusic = _registry?.GetMusicData(nextMusicId);
            if (jingle != null && nextMusic != null) PlayJingleThen(jingle, nextMusic, nextMusicFadeInDuration, overlapDuration);
        }
    }
}
