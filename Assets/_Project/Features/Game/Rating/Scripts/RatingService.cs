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
            _cloudSaveService = cloudSaveService ?? throw new System.ArgumentNullException(nameof(cloudSaveService));
            _gameStateReader = gameStateReader ?? throw new System.ArgumentNullException(nameof(gameStateReader));
            _gameManager = gameManager ?? throw new System.ArgumentNullException(nameof(gameManager));
        }

        public async Task<(int, int, int, int)> HandleGameFinished(GameResult result)
        {
            Debug.Log("[RatingService] HandleGameFinished invoked.");

            if (result.IsDraw)
            {
                Debug.Log("[RatingService] Game was a draw. No rating change.");
                return (0, 0, 0, 0); // Return no change
            }

            Debug.Log($"[RatingService] WinnerClientId from GameResult: {result.WinnerClientId}");

            if (_gameStateReader == null)
            {
                Debug.LogError("[RatingService] CRITICAL: _gameStateReader is null. Aborting.");
                return (0, 0, 0, 0);
            }

            Debug.Log($"[RatingService] _gameStateReader.PlayerDatas.Count: {_gameStateReader.PlayerDatas.Count}");
            foreach(var p in _gameStateReader.PlayerDatas)
            {
                Debug.Log($"[RatingService] Found player in list: ClientId={p.ClientId}, Name={p.PlayerName}");
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

            if (winnerData == null)
            {
                 Debug.LogError($"[RatingService] Could not find winner with ClientId {result.WinnerClientId} in PlayerDatas. Aborting.");
                 return (0, 0, 0, 0);
            }
            if (loserData == null)
            {
                 Debug.LogError("[RatingService] Could not determine loser from PlayerDatas. Aborting.");
                 return (0, 0, 0, 0);
            }

            Debug.Log($"[RatingService] Successfully found winner: {winnerData.Value.PlayerName} ({winnerData.Value.ClientId})");
            Debug.Log($"[RatingService] Successfully found loser: {loserData.Value.PlayerName} ({loserData.Value.ClientId})");

            if (_gameManager == null)
            {
                Debug.LogError("[RatingService] CRITICAL: _gameManager is null. Aborting.");
                return (0, 0, 0, 0);
            }

            string winnerAuthId = _gameManager.GetPlayerId(winnerData.Value.ClientId);
            string loserAuthId = _gameManager.GetPlayerId(loserData.Value.ClientId);

            Debug.Log($"[RatingService] Fetched Auth IDs: winnerAuthId='{winnerAuthId}', loserAuthId='{loserAuthId}'");

            if (string.IsNullOrEmpty(winnerAuthId) || string.IsNullOrEmpty(loserAuthId))
            {
                Debug.LogError("[RatingService] Could not find AuthenticationId for a client. Aborting rating change.");
                return (0, 0, 0, 0);
            }

            if (_cloudSaveService == null)
            {
                Debug.LogError("[RatingService] CRITICAL: _cloudSaveService is null. Aborting.");
                return (0, 0, 0, 0);
            }

            Debug.Log("[RatingService] Calling _cloudSaveService.GetRatingAsync...");
            // Load ratings directly using the new service method
            int oldWinnerRating = await _cloudSaveService.GetRatingAsync(winnerAuthId);
            int oldLoserRating = await _cloudSaveService.GetRatingAsync(loserAuthId);
            Debug.Log($"[RatingService] Ratings loaded: Winner={oldWinnerRating}, Loser={oldLoserRating}");

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

            Debug.Log("[RatingService] Calling _cloudSaveService.UpdateRatingsAsync...");
            // Atomically update both players' ratings and leaderboard scores with a single call
            await _cloudSaveService.UpdateRatingsAsync(winnerAuthId, loserAuthId, newWinnerRating, newLoserRating);
            Debug.Log("[RatingService] HandleGameFinished completed successfully.");

            return (oldWinnerRating, newWinnerRating, oldLoserRating, newLoserRating);
        }
    }
}