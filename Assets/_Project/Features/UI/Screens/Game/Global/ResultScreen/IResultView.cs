using System;
using static TypingSurvivor.Features.Game.Gameplay.GameManager;

namespace TypingSurvivor.Features.UI.Screens.Result
{
    public interface IResultView
    {
        event Action OnRematchClicked;
        event Action OnMainMenuClicked;

        void ShowAndPlaySequence(GameResultDto dto, float personalBest, int playerRank, int totalPlayers);
    }
}
