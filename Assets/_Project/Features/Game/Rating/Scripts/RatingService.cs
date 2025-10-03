using System.Threading.Tasks;
using TypingSurvivor.Features.Core.CloudSave;
using TypingSurvivor.Features.Game.Gameplay;
using TypingSurvivor.Features.Game.Gameplay.Data;
using UnityEngine;

namespace TypingSurvivor.Features.Game.Rating
{
    public class RatingService : IRatingService
    {
        private readonly ICloudSaveService _cloudSaveService;
        private readonly IGameStateReader _gameStateReader;

        private const int RatingChangeAmount = 10; // Simple +/- for now

        public RatingService(ICloudSaveService cloudSaveService, IGameStateReader gameStateReader)
        {
            _cloudSaveService = cloudSaveService;
            _gameStateReader = gameStateReader;
        }

        public async Task HandleGameFinished(GameResult result)
        {
            if (result.IsDraw)
            {
                Debug.Log("[RatingService] Game was a draw. No rating change.");
                return;
            }

            // Identify winner and loser
            ulong winnerClientId = result.WinnerClientId;
            ulong loserClientId = 0;

            foreach (var player in _gameStateReader.PlayerDatas)
            {
                if (player.ClientId != winnerClientId)
                {
                    loserClientId = player.ClientId;
                    break;
                }
            }

            if (loserClientId == 0 && _gameStateReader.PlayerDatas.Count > 1)
            {
                Debug.LogError($"[RatingService] Could not find loser. Winner was {winnerClientId}.");
                return;
            }

            // --- Placeholder for ClientId -> PlayerId mapping ---
            // In a real dedicated server, the server would have a mapping of Netcode ClientIds to UGS PlayerIds.
            // For now, we will ASSUME ClientId can be converted directly to PlayerId for testing purposes.
            string winnerPlayerId = winnerClientId.ToString();
            string loserPlayerId = loserClientId.ToString();
            Debug.LogWarning($"[RatingService] Using a placeholder mapping from ClientId to PlayerId. This may not work in a real server environment.");
            // --- End Placeholder ---

            // Load data for both players
            var winnerData = await _cloudSaveService.LoadPlayerDataForPlayerAsync(winnerPlayerId);
            var loserData = await _cloudSaveService.LoadPlayerDataForPlayerAsync(loserPlayerId);

            // If data doesn't exist, create it with default values
            winnerData ??= new PlayerSaveData();
            loserData ??= new PlayerSaveData();

            // Calculate new ratings
            int oldWinnerRating = winnerData.Rating;
            int oldLoserRating = loserData.Rating;

            winnerData.Rating += RatingChangeAmount;
            loserData.Rating = Mathf.Max(0, loserData.Rating - RatingChangeAmount); // Prevent negative rating

            Debug.Log($"[RatingService] Winner ({winnerPlayerId}): {oldWinnerRating} -> {winnerData.Rating}");
            Debug.Log($"[RatingService] Loser ({loserPlayerId}): {oldLoserRating} -> {loserData.Rating}");

            // Save data for both players
            await _cloudSaveService.SavePlayerDataForPlayerAsync(winnerPlayerId, winnerData);
            await _cloudSaveService.SavePlayerDataForPlayerAsync(loserPlayerId, loserData);
        }
    }
}
