using System.Collections.Generic;
using UnityEngine;

namespace TypingSurvivor.Features.Core.CloudSave
{
    [System.Serializable]
    public class PlayerSaveData
    {
        public string PlayerName; // For UI display
        public int SaveVersion = 1;
        public PlayerSettingsData Settings;
        public PlayerProgressData Progress;

        // Default constructor for deserialization
        public PlayerSaveData()
        {
            Settings = new PlayerSettingsData();
            Progress = new PlayerProgressData();
        }

        // Constructor for profile creation
        public PlayerSaveData(string playerName)
        {
            PlayerName = playerName;
            Settings = new PlayerSettingsData();
            Progress = new PlayerProgressData();
        }
    }

    [System.Serializable]
    public class PlayerSettingsData
    {
        public float MasterVolume = 1.0f;
        
        [Range(0.0f, 1.0f)]
        public float BgmVolume = 0.8f;

        [Range(0.0f, 1.0f)]
        public float SfxVolume = 1.0f;

        public string KeybindingsOverrideJson;
    }

    [System.Serializable]
    public class PlayerProgressData
    {
        public int Rating = 1500; // Default starting rating
        public float SinglePlayHighScore = 0f;
    }
}
