using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TypingSurvivor.Features.Core.Leaderboard.Rating; // For LeaderboardEntry
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using UnityEngine;

namespace TypingSurvivor.Features.Core.Leaderboard
{
    /// <summary>
    /// Client-specific implementation of ISurvivalLeaderboardService that uses the Leaderboards SDK.
    /// </summary>
    public class ClientSurvivalLeaderboardService : ISurvivalLeaderboardService
    {
        private const string SurvivalLeaderboardId = "SURVIVAL_TIME_LEADERBOARD";

        public async Task SubmitScoreAsync(float survivalTime)
        {
            await LeaderboardsService.Instance.AddPlayerScoreAsync(SurvivalLeaderboardId, survivalTime);
        }

        public async Task<(int playerRank, int totalPlayers)> GetPlayerRankAsync()
        {
            try
            {
                var scoreResponse = await LeaderboardsService.Instance.GetPlayerScoreAsync(SurvivalLeaderboardId);
                var scoresPage = await LeaderboardsService.Instance.GetScoresAsync(SurvivalLeaderboardId, new GetScoresOptions { Limit = 1 });
                return (scoreResponse?.Rank + 1 ?? 0, scoresPage.Total);
            }
            catch (LeaderboardsException e)
            {
                if (e.Reason == LeaderboardsExceptionReason.NotFound)
                {
                    return (0, 0);
                }
                Debug.LogError(e);
                return (0, 0);
            }
        }

        public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(int offset, int limit)
        {
            var options = new GetScoresOptions { Offset = offset, Limit = limit };
            var scoresPage = await LeaderboardsService.Instance.GetScoresAsync(SurvivalLeaderboardId, options);

            return scoresPage.Results.Select(score => new LeaderboardEntry
            {
                Rank = score.Rank + 1, 
                PlayerName = score.PlayerName,
                Score = (int)score.Score
            }).ToList();
        }
    }
}
