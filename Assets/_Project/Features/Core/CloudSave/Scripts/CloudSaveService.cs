using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Unity.Services.CloudCode;
using UnityEngine;
using Unity.Services.Core;

#if UNITY_SERVER
using System.Net.Http;
using Unity.Services.Authentication.Server;
using Newtonsoft.Json;
#endif

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
#if UNITY_SERVER
        private readonly HttpClient _httpClient;
#endif

        public CloudSaveService()
        {
#if UNITY_SERVER
            _httpClient = new HttpClient();
#endif
        }

        public async Task UpdateRatingsAsync(string winnerId, string loserId, int newWinnerRating, int newLoserRating)
        {
#if UNITY_SERVER
            try
            {
                var projectId = Application.cloudProjectId;
                var accessToken = ServerAuthenticationService.Instance.AccessToken;

                var request = new HttpRequestMessage(HttpMethod.Post, $"https://cloud-code.services.api.unity.com/v1/projects/{projectId}/scripts/UpdateRatings");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var args = new Dictionary<string, object>
                {
                    { "winnerId", winnerId },
                    { "loserId", loserId },
                    { "newWinnerRating", newWinnerRating },
                    { "newLoserRating", newLoserRating }
                };

                // Unityの標準JsonUtilityはDictionaryを直接シリアライズできないため、手動でJSON文字列を構築するか、
                // Newtonsoft.Jsonなどのライブラリを使用する必要があります。
                // ここでは、プロジェクトにNewtonsoft.Jsonがインポートされていることを前提とします。
                var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(new { @params = args });
                Debug.Log($"[CloudSaveService] UpdateRatings payload: {jsonPayload}");
                request.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                Debug.Log("[CloudSaveService] Successfully called UpdateRatings Cloud Code.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveService] Failed to call UpdateRatings Cloud Code: {e}");
            }
#else
            Debug.LogError("UpdateRatingsAsync should only be called from the server.");
            await Task.CompletedTask;
#endif
        }

        public async Task<int> GetRatingAsync(string playerId)
        {
#if UNITY_SERVER
            // For server-side calls, use the server authentication token.
            try
            {
                var projectId = Application.cloudProjectId;
                var accessToken = ServerAuthenticationService.Instance.AccessToken;

                var request = new HttpRequestMessage(HttpMethod.Post, $"https://cloud-code.services.api.unity.com/v1/projects/{projectId}/scripts/GetRating");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var args = new Dictionary<string, object> { { "targetPlayerId", playerId } };
                var jsonPayload = JsonConvert.SerializeObject(new { @params = args });
                Debug.Log($"[CloudSaveService] GetRating payload: {jsonPayload}");
                request.Content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<GetRatingResult>(responseBody);
                return result.Rating;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSaveService] Failed to call GetRating Cloud Code (as Server) for player {playerId}: {e}");
                return 1500; // Return default rating on failure
            }
#else
            // For client-side calls, use the player authentication provided by the SDK.
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
                Debug.LogError($"[CloudSaveService] Failed to call GetRating Cloud Code (as Client) for player {playerId}: {e}");
                // Return default rating on failure
                return 1500;
            }
#endif
        }

        // Helper class to deserialize the result from GetRating Cloud Code
        private class GetRatingResult
        {
            public int Rating { get; set; }
        }


    }
}