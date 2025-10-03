using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Api;

// This namespace can be whatever you want, it's for organizing your Cloud Code modules.
namespace TypingSurvivor.CloudCode
{
    public class RatingModule
    {
        // The ICloudSaveDataService will be automatically injected by the Cloud Code runtime.
        private readonly ICloudSaveDataService _cloudSave;

        public RatingModule(ICloudSaveDataService cloudSave)
        {
            _cloudSave = cloudSave;
        }

        /// <summary>
        /// A Cloud Code function to save data for a specific player.
        /// This runs on the server and has privileged access.
        /// </summary>
        [CloudCodeFunction("SavePlayerData")]
        public async Task SavePlayerData(IExecutionContext context, string playerId, string playerDataKey, JsonElement playerData)
        {
            var data = new Dictionary<string, JsonElement>
            {
                { playerDataKey, playerData }
            };
            // Use the server-authoritative SetItemAsync API
            await _cloudSave.SetItemAsync(context, playerId, data);
        }

        /// <summary>
        /// A Cloud Code function to load data for a specific player.
        /// </summary>
        [CloudCodeFunction("LoadPlayerData")]
        public async Task<JsonElement> LoadPlayerData(IExecutionContext context, string playerId, string playerDataKey)
        {
            var keys = new HashSet<string> { playerDataKey };
            var result = await _cloudSave.GetItemsAsync(context, playerId, keys);

            if (result.TryGetValue(playerDataKey, out var item))
            {
                return item.Value;
            }

            // If the key is not found, throw a specific exception that the client can catch.
            throw new CloudCodeException(CloudCodeExceptionReason.NotFound, "Player data not found.");
        }
    }
}
