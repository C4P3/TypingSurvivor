using System.Collections;
using System.Collections.Generic;
using TypingSurvivor.Features.Core.Audio.Data;
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
        
        // 変更点1: AudioRegistryを保持するためのフィールドを追加
        private AudioRegistry _registry;

        private readonly Stack<MusicData> _musicStack = new();
        private Coroutine _activeTransitionCoroutine;

        // 変更点2: AudioRegistryを受け取る初期化メソッド
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

        // --- MusicDataを受け取る元のAPI ---

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
        public void PlayJingleThen(MusicData jingle, MusicData nextMusic, float nextMusicFadeInDuration = 1.0f)
        {
            StopAndClear();
            _musicStack.Push(nextMusic); // The next music is the new base
            _activeTransitionCoroutine = StartCoroutine(JingleThenCoroutine(jingle, nextMusic, nextMusicFadeInDuration));
        }

        // --- SoundIdを受け取る新しい利便性の高いAPI ---

        /// <summary>
        /// Master command. Interrupts everything and plays the specified music by ID.
        /// Clears the entire music stack.
        /// </summary>
        public void Play(SoundId musicId, float fadeDuration = 0.0f)
        {
            var music = _registry?.GetMusicData(musicId);
            if (music != null) Play(music, fadeDuration);
        }
        
        /// <summary>
        /// Pushes a temporary music track onto the stack and plays it by ID.
        /// </summary>
        public void Push(SoundId temporaryMusicId, float fadeDuration = 1.0f)
        {
            var music = _registry?.GetMusicData(temporaryMusicId);
            if (music != null) Push(music, fadeDuration);
        }

        /// <summary>
        /// Plays a non-looping jingle (by ID), and then transitions to the next music track (by ID).
        /// </summary>
        public void PlayJingleThen(SoundId jingleId, SoundId nextMusicId, float nextMusicFadeInDuration = 1.0f)
        {
            var jingle = _registry?.GetMusicData(jingleId);
            var nextMusic = _registry?.GetMusicData(nextMusicId);
            if (jingle != null && nextMusic != null) PlayJingleThen(jingle, nextMusic, nextMusicFadeInDuration);
        }


        /// <summary>
        /// Stops all music playback with a fade-out and clears the stack.
        /// </summary>
        public void Stop(float fadeDuration)
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
            _musicStack.Clear();
        }

        private IEnumerator CrossfadeCoroutine(MusicData music, float duration)
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
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;
                
                // Lerpを使って滑らかにクロスフェード
                newTurntable.volume = Mathf.Lerp(0, music.Volume, progress);
                oldTurntable.volume = Mathf.Lerp(oldTurntable.volume, 0, progress);
                yield return null;
            }

            newTurntable.volume = music.Volume;
            oldTurntable.Stop();
            oldTurntable.clip = null;
            _activeTransitionCoroutine = null;
        }

        private IEnumerator JingleThenCoroutine(MusicData jingle, MusicData nextMusic, float nextMusicFadeInDuration)
        {
            // Fade out current music (if any)
            if (_activeTurntable.isPlaying)
            {
                yield return StartCoroutine(FadeOutCoroutine(0.5f));
            }

            // Play jingle
            _activeTurntable.clip = jingle.Clip;
            _activeTurntable.volume = jingle.Volume;
            _activeTurntable.loop = false;
            _activeTurntable.Play();

            yield return new WaitForSeconds(jingle.Clip.length);

            // Crossfade to next music
            yield return StartCoroutine(CrossfadeCoroutine(nextMusic, nextMusicFadeInDuration));
        }

        private IEnumerator FadeOutCoroutine(float duration)
        {
            float startVolume = _activeTurntable.volume;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                _activeTurntable.volume = Mathf.Lerp(startVolume, 0, timer / duration);
                yield return null;
            }
            _activeTurntable.volume = 0;
            _activeTurntable.Stop();
            _activeTurntable.clip = null;
            _activeTransitionCoroutine = null; // FadeOutで停止した際も念のためコルーチン参照をクリア
        }
        
        public void SetPitch(float pitch)
        {
            // 両方のターンテーブルにピッチを設定することで、クロスフェード中もピッチが一貫するようにする
            _turntableA.pitch = pitch;
            _turntableB.pitch = pitch;
        }

        public void ResetPitch()
        {
            SetPitch(1.0f);
        }
    }
}
