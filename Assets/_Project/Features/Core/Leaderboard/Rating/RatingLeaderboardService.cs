
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode;
using UnityEngine;

namespace TypingSurvivor.Features.Core.Leaderboard.Rating
{
    public class RatingLeaderboardService : IRatingLeaderboardService
    {
        private class CloudLeaderboardEntry
        {
            public int rank { get; set; }
            public string playerName { get; set; }
            public int score { get; set; }
        }

        private class CloudLeaderboardResult
        {
            public List<CloudLeaderboardEntry> results { get; set; }
        }

        public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(int offset, int limit)
        {
            try
            {
                var args = new Dictionary<string, object>
                {
                    { "offset", offset },
                    { "limit", limit }
                };

                var result = await CloudCodeService.Instance.CallEndpointAsync<CloudLeaderboardResult>("GetLeaderboard", args);

                var leaderboardEntries = new List<LeaderboardEntry>();
                if (result != null && result.results != null)
                {
                    foreach (var cloudEntry in result.results)
                    {
                        leaderboardEntries.Add(new LeaderboardEntry
                        {
                            Rank = cloudEntry.rank,
                            PlayerName = cloudEntry.playerName,
                            Score = cloudEntry.score
                        });
                    }
                }
                return leaderboardEntries;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RatingLeaderboardService] Failed to get leaderboard: {e.Message}");
                return new List<LeaderboardEntry>(); // Return empty list on error
            }
        }
    }
}
