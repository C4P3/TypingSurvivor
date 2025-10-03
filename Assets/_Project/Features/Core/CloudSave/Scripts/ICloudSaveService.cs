using System.Threading.Tasks;

namespace TypingSurvivor.Features.Core.CloudSave
{
    public interface ICloudSaveService
    {
        /// <summary>
        /// Saves the entire PlayerSaveData object to the cloud.
        /// </summary>
        Task<bool> SavePlayerDataAsync(PlayerSaveData data);

        /// <summary>
        /// Loads the entire PlayerSaveData object from the cloud.
        /// </summary>
        /// <returns>The loaded PlayerSaveData, or null if not found.</returns>
        Task<PlayerSaveData> LoadPlayerDataAsync();
    }
}
