using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;

namespace TypingSurvivor.Features.Core.CloudSave
{
    public class CloudSaveService : ICloudSaveService
    {
        private const string PLAYER_DATA_KEY = "PLAYER_SAVE_DATA";

        public async Task<bool> SavePlayerDataAsync(PlayerSaveData data)
        {
            try
            {
                var dataToSave = new Dictionary<string, object> { { PLAYER_DATA_KEY, data } };
                await Unity.Services.CloudSave.CloudSaveService.Instance.Data.Player.SaveAsync(dataToSave);
                Debug.Log($"[CloudSaveService] Successfully saved player data.");
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
                else
                {
                    Debug.Log($"[CloudSaveService] No player data found for key: {PLAYER_DATA_KEY}");
                    return null;
                }
            }
            catch (CloudSaveException e) when (e.Reason == CloudSaveExceptionReason.NotFound)
            {
                Debug.Log($"[CloudSaveService] No player data found (exception): {e}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveService] Failed to load player data: {e}");
                return null;
            }
        }
    }
}
