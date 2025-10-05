using System.Threading.Tasks;

namespace TypingSurvivor.Features.Core.CloudSave
{
    public interface ICloudSaveService
    {
        // For client to save its own data
        Task<bool> SavePlayerDataAsync(PlayerSaveData data);

        // For client to load its own data
        Task<PlayerSaveData> LoadPlayerDataAsync();

        // For server to update ratings after a match
        Task UpdateRatingsAsync(string winnerId, string loserId, int newWinnerRating, int newLoserRating);

        // For server to load a player's rating
        Task<int> GetRatingAsync(string playerId);

    }
}
