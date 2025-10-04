using System.Collections.Generic;

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
    }

    [System.Serializable]
    public class PlayerProgressData
    {
        public int Rating = 1500; // Default starting rating
        public int SinglePlayHighScore = 0;
    }
}
