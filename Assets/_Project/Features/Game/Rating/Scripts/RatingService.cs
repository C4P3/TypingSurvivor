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
        private readonly GameManager _gameManager;

        private const int RatingChangeAmount = 10;

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

            // Get the real PlayerIds from the GameManager
            string winnerPlayerId = _gameManager.GetPlayerId(winnerClientId);
            string loserPlayerId = _gameManager.GetPlayerId(loserClientId);

            if (string.IsNullOrEmpty(winnerPlayerId) || string.IsNullOrEmpty(loserPlayerId))
            {
                Debug.LogError($"[RatingService] Could not find PlayerId for a client. Winner: {winnerPlayerId}, Loser: {loserPlayerId}. Aborting rating change.");
                return;
            }

            TypingSurvivor.Features.Core.CloudSave.PlayerSaveData winnerData = await _cloudSaveService.LoadPlayerDataForPlayerAsync(winnerPlayerId);
            TypingSurvivor.Features.Core.CloudSave.PlayerSaveData loserData = await _cloudSaveService.LoadPlayerDataForPlayerAsync(loserPlayerId);

            // If data doesn't exist, create it with default values
            if (winnerData == null)
            {
                winnerData = new TypingSurvivor.Features.Core.CloudSave.PlayerSaveData();
            }
            if (loserData == null)
            {
                loserData = new TypingSurvivor.Features.Core.CloudSave.PlayerSaveData();
            }

            int oldWinnerRating = winnerData.Rating;
            int oldLoserRating = loserData.Rating;

            winnerData.Rating += RatingChangeAmount;
            loserData.Rating = Mathf.Max(0, loserData.Rating - RatingChangeAmount);

            Debug.Log($"[RatingService] Winner ({winnerPlayerId}): {oldWinnerRating} -> {winnerData.Rating}");
            Debug.Log($"[RatingService] Loser ({loserPlayerId}): {oldLoserRating} -> {loserData.Rating}");

            await _cloudSaveService.SavePlayerDataForPlayerAsync(winnerPlayerId, winnerData);
            await _cloudSaveService.SavePlayerDataForPlayerAsync(loserPlayerId, loserData);
        }
    }
}