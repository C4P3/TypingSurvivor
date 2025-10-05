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

        public async Task<(int, int)> HandleGameFinished(GameResult result)
        {
            if (result.IsDraw)
            {
                Debug.Log("[RatingService] Game was a draw. No rating change.");
                return (0, 0); // Return no change
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
                return (0, 0);
            }

            string winnerAuthId = _gameManager.GetPlayerId(winnerData.Value.ClientId);
            string loserAuthId = _gameManager.GetPlayerId(loserData.Value.ClientId);

            if (string.IsNullOrEmpty(winnerAuthId) || string.IsNullOrEmpty(loserAuthId))
            {
                Debug.LogError("[RatingService] Could not find AuthenticationId for a client. Aborting rating change.");
                return (0, 0);
            }

            // Load ratings directly using the new service method
            int oldWinnerRating = await _cloudSaveService.GetRatingAsync(winnerAuthId);
            int oldLoserRating = await _cloudSaveService.GetRatingAsync(loserAuthId);

            double expectedWinner = 1.0 / (1.0 + System.Math.Pow(10, (double)(oldLoserRating - oldWinnerRating) / 400.0));

            int newWinnerRating = oldWinnerRating + (int)(K_FACTOR * (1.0 - expectedWinner));
            int newLoserRating = oldLoserRating - (int)(K_FACTOR * (1.0 - expectedWinner));

            // Ensure rating does not fall below zero.
            if (newLoserRating < 0)
            {
                newLoserRating = 0;
            }

            Debug.Log($"[RatingService] Winner ({winnerAuthId}): {oldWinnerRating} -> {newWinnerRating}");
            Debug.Log($"[RatingService] Loser ({loserAuthId}): {oldLoserRating} -> {newLoserRating}");

            // Atomically update both players' ratings and leaderboard scores with a single call
            await _cloudSaveService.UpdateRatingsAsync(winnerAuthId, loserAuthId, newWinnerRating, newLoserRating);

            return (newWinnerRating, newLoserRating);
        }
    }
}