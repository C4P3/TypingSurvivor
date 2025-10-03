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

        // --- Server-Side Methods (via Cloud Code) --- //
        public async Task SavePlayerDataForPlayerAsync(string playerId, PlayerSaveData data)
        {
            try
            {
                var args = new Dictionary<string, object>
                {
                    { "playerId", playerId },
                    { "playerDataKey", PLAYER_DATA_KEY },
                    { "playerData", data }
                };
                await CloudCodeService.Instance.CallEndpointAsync("SavePlayerData", args);
                Debug.Log($"[CloudSaveService] Successfully requested save for player {playerId}.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveService] Failed to call SavePlayerData Cloud Code for player {playerId}: {e}");
            }
        }

        public async Task<PlayerSaveData> LoadPlayerDataForPlayerAsync(string playerId)
        {
            try
            {
                var args = new Dictionary<string, object>
                {
                    { "playerId", playerId },
                    { "playerDataKey", PLAYER_DATA_KEY }
                };
                var result = await CloudCodeService.Instance.CallEndpointAsync<PlayerSaveData>("LoadPlayerData", args);
                return result;
            }
            catch (CloudCodeException e) when (e.Message.Contains("Not Found")) // Simple check for not found
            {
                 Debug.Log($"[CloudSaveService] No player data found for player {playerId} via Cloud Code.");
                 return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveService] Failed to call LoadPlayerData Cloud Code for player {playerId}: {e}");
                return null;
            }
        }
    }
}