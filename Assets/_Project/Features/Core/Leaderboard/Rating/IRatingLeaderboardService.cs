
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TypingSurvivor.Features.Core.Leaderboard.Rating
{
    public class LeaderboardEntry
    {
        public int Rank { get; set; }
        public string PlayerName { get; set; }
        public int Score { get; set; }
    }

    public interface IRatingLeaderboardService
    {
        Task<List<LeaderboardEntry>> GetLeaderboardAsync(int offset, int limit);
    }
}
