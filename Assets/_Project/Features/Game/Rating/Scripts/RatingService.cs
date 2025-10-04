using System.Threading.Tasks;
using UnityEngine;
using TypingSurvivor.Features.Core.CloudSave;
using TypingSurvivor.Features.Game.Gameplay;
using TypingSurvivor.Features.Game.Gameplay.Data;

namespace TypingSurvivor.Features.Game.Rating
{
    public class RatingService : IRatingService
    {
        private const int K_FACTOR = 32;
        private const int DEFAULT_RATING = 1500;

        private readonly ICloudSaveService _cloudSaveService;
        private readonly IGameStateReader _gameStateReader;
        private readonly GameManager _gameManager; // To get authentication IDs

        public RatingService(ICloudSaveService cloudSaveService, IGameStateReader gameStateReader, GameManager gameManager)
        {
            _cloudSaveService = cloudSaveService;
            _gameStateReader = gameStateReader;
            _gameManager = gameManager;
        }

        public async Task HandleGameFinished(GameResult result)
        {
            if (result.IsDraw)
            {
                Debug.Log("[RatingService] Game was a draw. No rating change.");
                return;
            }

            PlayerData? winnerData = null;
            PlayerData? loserData = null;

            foreach (var pData in _gameStateReader.PlayerDatas)
            {
                if (pData.ClientId == result.WinnerClientId)
                {
                    winnerData = pData;
                }
                else
                {
                    loserData = pData;
                }
            }

            if (winnerData == null || loserData == null)
            {
                Debug.LogError("[RatingService] Could not determine winner and loser. Aborting rating change.");
                return;
            }

            // Use the correct method name: GetPlayerId
            string winnerAuthId = _gameManager.GetPlayerId(winnerData.Value.ClientId);
            string loserAuthId = _gameManager.GetPlayerId(loserData.Value.ClientId);

            if (string.IsNullOrEmpty(winnerAuthId) || string.IsNullOrEmpty(loserAuthId))
            {
                Debug.LogError("[RatingService] Could not find AuthenticationId for a client. Aborting rating change.");
                return;
            }

            // Use a safe null check instead of the '??' operator to avoid compiler issues
            var winnerSaveData = await _cloudSaveService.LoadPlayerDataForPlayerAsync(winnerAuthId);
            if (winnerSaveData == null) winnerSaveData = new PlayerSaveData();

            var loserSaveData = await _cloudSaveService.LoadPlayerDataForPlayerAsync(loserAuthId);
            if (loserSaveData == null) loserSaveData = new PlayerSaveData();

            int oldWinnerRating = winnerSaveData.Progress.Rating > 0 ? winnerSaveData.Progress.Rating : DEFAULT_RATING;
            int oldLoserRating = loserSaveData.Progress.Rating > 0 ? loserSaveData.Progress.Rating : DEFAULT_RATING;

            double expectedWinner = 1.0 / (1.0 + System.Math.Pow(10, (double)(oldLoserRating - oldWinnerRating) / 400.0));

            winnerSaveData.Progress.Rating = oldWinnerRating + (int)(K_FACTOR * (1.0 - expectedWinner));
            loserSaveData.Progress.Rating = oldLoserRating - (int)(K_FACTOR * expectedWinner);

            Debug.Log($"[RatingService] Winner ({winnerAuthId}): {oldWinnerRating} -> {winnerSaveData.Progress.Rating}");
            Debug.Log($"[RatingService] Loser ({loserAuthId}): {oldLoserRating} -> {loserSaveData.Progress.Rating}");

            await _cloudSaveService.SavePlayerDataForPlayerAsync(winnerAuthId, winnerSaveData);
            await _cloudSaveService.SavePlayerDataForPlayerAsync(loserAuthId, loserSaveData);
        }
    }
}