using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TypingSurvivor.Features.Core.Leaderboard;
using Unity.Services.CloudCode;
using UnityEngine;

namespace TypingSurvivor.Features.Core.Leaderboard
{
    public class SurvivalLeaderboardService : ISurvivalLeaderboardService
    {
        // Helper classes to deserialize the result from Cloud Code
        private class GetRankResult
        {
            public int playerRank { get; set; }
            public int totalPlayers { get; set; }
        }

        public async Task SubmitScoreAsync(float survivalTime)
        {
            try
            {
                var args = new Dictionary<string, object>
                {
                    { "survivalTime", survivalTime }
                };
                await CloudCodeService.Instance.CallEndpointAsync("SubmitSurvivalScore", args);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SurvivalLeaderboardService] Failed to submit score: {e.Message}");
            }
        }

        public async Task<(int playerRank, int totalPlayers)> GetPlayerRankAsync()
        {
            try
            {
                var result = await CloudCodeService.Instance.CallEndpointAsync<GetRankResult>("GetSurvivalRank");
                return (result.playerRank, result.totalPlayers);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SurvivalLeaderboardService] Failed to get player rank: {e.Message}");
                return (0, 0); // Return default/error values
            }
        }
    }
}
