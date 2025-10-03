using System.Collections.Generic;
using TypingSurvivor.Features.Core.Audio.Data;
using UnityEngine;

namespace TypingSurvivor.Features.Core.Audio
{
    // SoundId enum remains the same, acting as a universal identifier.
    public enum SoundId
    {
        None = 0,
        // UI
        UIButtonClick = 10,
        UIButtonHover = 11,
        // Gameplay
        Countdown = 100,
        CountdownEnd = 101,
        TypingKeyPress = 102,
        TypingSuccess = 103,
        TypingMiss = 104,
        BlockDestroy = 105,
        // Items
        ItemPickup = 200,
        BombExplosion = 201,
        LightningStrike = 202,
        RocketLaunch = 203,
        HealEffect = 204,
        SpeedUpEffect = 205,
        StarActivate = 206,
        PoisonEffect = 207,
        UnchiEffect = 208,
        // BGM & Jingles
        TitleMusic = 299,
        MainMenuMusic = 300,
        GameMusic = 301,
        ResultsMusic = 302,
        WinJingle = 303,
        LoseJingle = 304,
        
        // 新しいIDを追加する際は、既存の値と被らないように注意する
        // 例: NewSound = 106,
    }

    [System.Serializable]
    public class SfxEntry
    {
        public SoundId Id;
        public SoundEffectData SoundData;
    }

    [System.Serializable]
    public class MusicEntry
    {
        public SoundId Id;
        public MusicData MusicData;
    }

    [CreateAssetMenu(fileName = "AudioRegistry", menuName = "Typing Survivor/Audio/Audio Registry")]
    public class AudioRegistry : ScriptableObject
    {
        [Header("Sound Effects")]
        [SerializeField] private List<SfxEntry> _sfxEntries;

        [Header("Music & Jingles")]
        [SerializeField] private List<MusicEntry> _musicEntries;

        private Dictionary<SoundId, SoundEffectData> _sfxDictionary;
        private Dictionary<SoundId, MusicData> _musicDictionary;

        public void Initialize()
        {
            _sfxDictionary = new Dictionary<SoundId, SoundEffectData>();
            foreach (var entry in _sfxEntries)
            {
                if (entry.SoundData != null && !_sfxDictionary.ContainsKey(entry.Id))
                {
                    _sfxDictionary.Add(entry.Id, entry.SoundData);
                }
            }

            _musicDictionary = new Dictionary<SoundId, MusicData>();
            foreach (var entry in _musicEntries)
            {
                if (entry.MusicData != null && !_musicDictionary.ContainsKey(entry.Id))
                {
                    _musicDictionary.Add(entry.Id, entry.MusicData);
                }
            }
        }

        public SoundEffectData GetSfxData(SoundId id)
        {
            _sfxDictionary.TryGetValue(id, out var data);
            if (data == null) Debug.LogWarning($"SFX data for ID '{id}' not found in AudioRegistry.");
            return data;
        }

        public MusicData GetMusicData(SoundId id)
        {
            _musicDictionary.TryGetValue(id, out var data);
            if (data == null) Debug.LogWarning($"Music data for ID '{id}' not found in AudioRegistry.");
            return data;
        }
    }
}
