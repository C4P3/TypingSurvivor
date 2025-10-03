using System.Threading.Tasks;

namespace TypingSurvivor.Features.Core.CloudSave
{
    public interface ICloudSaveService
    {
        // For client to save its own data
        Task<bool> SavePlayerDataAsync(PlayerSaveData data);

        // For client to load its own data
        Task<PlayerSaveData> LoadPlayerDataAsync();

        // For server to save any player's data
        Task SavePlayerDataForPlayerAsync(string playerId, PlayerSaveData data);

        // For server to load any player's data
        Task<PlayerSaveData> LoadPlayerDataForPlayerAsync(string playerId);
    }
}
