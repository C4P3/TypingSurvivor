using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;

using Unity.Services.CloudCode;
using UnityEngine;

namespace TypingSurvivor.Features.Core.CloudSave
{
    public class CloudSaveService : ICloudSaveService
    {
        private const string PLAYER_DATA_KEY = "PLAYER_SAVE_DATA";

        // --- Client-Side Methods --- //
        public async Task<bool> SavePlayerDataAsync(PlayerSaveData data)
        {
            try
            {
                var dataToSave = new Dictionary<string, object> { { PLAYER_DATA_KEY, data } };
                await Unity.Services.CloudSave.CloudSaveService.Instance.Data.Player.SaveAsync(dataToSave);
                Debug.Log($"[CloudSaveService] Successfully saved player data for current player.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveService] Failed to save player data: {e}");
                return false;
            }
        }

        public async Task<PlayerSaveData> LoadPlayerDataAsync()
        {
            try
            {
                var results = await Unity.Services.CloudSave.CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { PLAYER_DATA_KEY });
                if (results.TryGetValue(PLAYER_DATA_KEY, out var item))
                {
                    return item.Value.GetAs<PlayerSaveData>();
                }
                Debug.Log($"[CloudSaveService] No player data found for current player.");
                return null;
            }
            catch (CloudSaveException e) when (e.Reason == CloudSaveExceptionReason.NotFound)
            {
                Debug.Log($"[CloudSaveService] No player data found for current player (exception): {e}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveService] Failed to load player data: {e}");
                return null;
            }
        }
        // --- Server-Side Methods (via Cloud Code) ---
        public async Task UpdateRatingsAsync(string winnerId, string loserId, int newWinnerRating, int newLoserRating)
        {
            try
            {
                var args = new Dictionary<string, object>
                {
                    { "winnerId", winnerId },
                    { "loserId", loserId },
                    { "newWinnerRating", newWinnerRating },
                    { "newLoserRating", newLoserRating }
                };
                await CloudCodeService.Instance.CallEndpointAsync("UpdateRatings", args);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveService] Failed to call UpdateRatings Cloud Code: {e}");
            }
        }

        public async Task<int> GetRatingAsync(string playerId)
        {
            try
            {
                var args = new Dictionary<string, object>
                {
                    { "targetPlayerId", playerId }
                };
                var result = await CloudCodeService.Instance.CallEndpointAsync<GetRatingResult>("GetRating", args);
                return result.Rating;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveService] Failed to call GetRating Cloud Code for player {playerId}: {e}");
                // Return default rating on failure
                return 1500;
            }
        }

        // Helper class to deserialize the result from GetRating Cloud Code
        private class GetRatingResult
        {
            public int Rating { get; set; }
        }


    }
}