using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Leaderboards;

namespace TypingSurvivor.Features.Core.Leaderboard.Rating
{
    /// <summary>
    /// Client-specific implementation of IRatingLeaderboardService that uses the Leaderboards SDK.
    /// </summary>
    public class ClientRatingLeaderboardService : IRatingLeaderboardService
    {
        private const string RatingLeaderboardId = "RATING_LEADERBOARD";

        public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(int offset, int limit)
        {
            var options = new GetScoresOptions { Offset = offset, Limit = limit };
            var scoresPage = await LeaderboardsService.Instance.GetScoresAsync(RatingLeaderboardId, options);

            return scoresPage.Results.Select(score => new LeaderboardEntry
            {
                Rank = score.Rank + 1, // SDK Rank is 0-based
                PlayerName = score.PlayerName,
                Score = (int)score.Score
            }).ToList();
        }
    }
}
