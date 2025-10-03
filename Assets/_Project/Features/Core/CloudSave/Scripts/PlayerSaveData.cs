namespace TypingSurvivor.Features.Core.CloudSave
{
    /// <summary>
    /// Defines the data structure for all player-specific information to be saved in the cloud.
    /// </summary>
    [System.Serializable]
    public class PlayerSaveData
    {
        public string PlayerName;
        public int Rating = 1500; // Default starting rating

        // Default constructor for deserialization
        public PlayerSaveData()
        {
        }

        public PlayerSaveData(string playerName)
        {
            PlayerName = playerName;
        }
    }
}
