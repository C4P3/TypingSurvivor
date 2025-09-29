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
        ResultsBGM
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

        public void Initialize()
        {
            _soundDictionary = new Dictionary<SoundId, AudioClip>();
            foreach (var entry in _sounds)
            {
                if (entry.Clip != null && !_soundDictionary.ContainsKey(entry.Id))
                {
                    _soundDictionary.Add(entry.Id, entry.Clip);
                }
            }
        }

        public AudioClip GetClip(SoundId id)
        {
            _soundDictionary.TryGetValue(id, out var clip);
            return clip;
        }
    }
}
