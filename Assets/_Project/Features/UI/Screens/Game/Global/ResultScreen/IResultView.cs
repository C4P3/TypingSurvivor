using System;
using static TypingSurvivor.Features.Game.Gameplay.GameManager;

namespace TypingSurvivor.Features.UI.Screens.Result
{
    public interface IResultView
    {
        event Action OnRematchClicked;
        event Action OnMainMenuClicked;

        void ShowAndPlaySequence(GameResultDto dto, float personalBest, int playerRank, int totalPlayers);

        /// <summary>
        /// 再戦タイマーの残り時間をビューに通知します。
        /// </summary>
        /// <param name="remainingTime">残り時間（秒）。無期限の場合は負の値。</param>
        void UpdateRematchTimer(float remainingTime);

        /// <summary>
        /// 他のプレイヤーが切断したことをビューに通知します。
        /// </summary>
        void NotifyOpponentDisconnected();
    }
}
