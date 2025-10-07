using System.Threading.Tasks;

namespace TypingSurvivor.Features.Core.Leaderboard
{
    public interface ISurvivalLeaderboardService
    {
        /// <summary>
        /// 指定したスコア（生存時間）をリーダーボードに送信します。
        /// </summary>
        Task SubmitScoreAsync(float survivalTime);

        /// <summary>
        /// 現在のプレイヤーのリーダーボード上のランク情報を取得します。
        /// </summary>
        /// <returns>プレイヤーの順位と、リーダーボードの総エントリー数を含むタプル。</returns>
        Task<(int playerRank, int totalPlayers)> GetPlayerRankAsync();
    }
}
