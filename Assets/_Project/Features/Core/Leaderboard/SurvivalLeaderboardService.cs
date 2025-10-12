using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TypingSurvivor.Features.Core.Leaderboard.Rating;
using Unity.Services.CloudCode;
using UnityEngine;

namespace TypingSurvivor.Features.Core.Leaderboard
{
    public class SurvivalLeaderboardService : ISurvivalLeaderboardService
    {
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

        public Task<List<LeaderboardEntry>> GetLeaderboardAsync(int offset, int limit)
        {
            // This is not implemented for the server-side service.
            // The server-side logic might not need to fetch leaderboards in this way.
            throw new NotImplementedException();
        }
    }
}
