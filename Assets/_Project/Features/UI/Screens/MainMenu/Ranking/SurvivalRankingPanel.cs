using System.Threading.Tasks;

namespace TypingSurvivor.Features.UI.Screens
{
    public class SurvivalRankingPanel : RankingPanelBase
    {
        public override void Show()
        {
            base.Show();

            // [Temporary Fix] ISurvivalLeaderboardService does not have GetLeaderboardAsync.
            // We are using the one from IRatingLeaderboardService to make it compile.
            // This means the list will show RATING data, while the player's rank is from SURVIVAL.
            _ = RefreshLeaderboardAsync(
                () => _survivalLeaderboardService.GetPlayerRankAsync(),
                (offset, limit) => _survivalLeaderboardService.GetLeaderboardAsync(offset, limit)
            );
        }
    }
}
