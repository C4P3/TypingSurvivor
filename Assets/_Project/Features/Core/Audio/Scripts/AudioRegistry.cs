using System.Collections.Generic;
using UnityEngine;

namespace TypingSurvivor.Features.Core.Audio
{
    public enum SoundId
    {
        None,
        // UI
        UIButtonClick,
        UIButtonHover,
        // Gameplay
        TypingSuccess,
        TypingMiss,
        BlockDestroy,
        // Items
        ItemPickup,
        BombExplosion,
        // BGM
        MainMenuBGM,
        GameBGM,
        ResultsBGM,
        // Jingles
        WinJingle,
        LoseJingle
    }

    [System.Serializable]
    public class SoundEntry
    {
        public SoundId Id;
        public AudioClip Clip;
    }

    [CreateAssetMenu(fileName = "AudioRegistry", menuName = "Typing Survivor/Audio/Audio Registry")]
    public class AudioRegistry : ScriptableObject
    {
        [SerializeField] private List<SoundEntry> _sounds;

        private Dictionary<SoundId, AudioClip> _soundDictionary;
        private Dictionary<AudioClip, SoundId> _clipToIdDictionary;

        public void Initialize()
        {
            _soundDictionary = new Dictionary<SoundId, AudioClip>();
            _clipToIdDictionary = new Dictionary<AudioClip, SoundId>();
            foreach (var entry in _sounds)
            {
                if (entry.Clip != null && !_soundDictionary.ContainsKey(entry.Id))
                {
                    _soundDictionary.Add(entry.Id, entry.Clip);
                    _clipToIdDictionary.Add(entry.Clip, entry.Id);
                }
            }
        }

        public AudioClip GetClip(SoundId id)
        {
            _soundDictionary.TryGetValue(id, out var clip);
            return clip;
        }

        public SoundId GetId(AudioClip clip)
        {
            _clipToIdDictionary.TryGetValue(clip, out var id);
            return id;
        }
    }
}
