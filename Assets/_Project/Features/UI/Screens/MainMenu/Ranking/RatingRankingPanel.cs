using System.Threading.Tasks;

namespace TypingSurvivor.Features.UI.Screens
{
    public class RatingRankingPanel : RankingPanelBase
    {
        public override void Show()
        {
            base.Show();
            
            // IRatingLeaderboardService doesn't have GetPlayerRankAsync, so we simulate it.
            // This means the "Top 7" rule won't apply to rating leaderboard for now.
            Task<(int, int)> getPlayerRank_dummy = Task.FromResult((0, 0));

            _ = RefreshLeaderboardAsync(
                () => getPlayerRank_dummy,
                (offset, limit) => _ratingLeaderboardService.GetLeaderboardAsync(offset, limit)
            );
        }
    }
}
