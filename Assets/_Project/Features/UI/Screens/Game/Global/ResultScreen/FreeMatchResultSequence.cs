using System.Collections;
using static TypingSurvivor.Features.Game.Gameplay.GameManager;

namespace TypingSurvivor.Features.UI.Screens
{
    public class FreeMatchResultSequence : IResultSequenceStrategy
    {
        public IEnumerator ExecuteSequence(ResultScreen context, GameResultDto dto, float personalBest, int playerRank, int totalPlayers)
        {
            context.HideAllCanvasGroups();
            context.SetSkipButtonActive(true);

            // Step 1: Show Win/Loss Banner
            yield return context.FadeCanvasGroup(context._winLoseBannerCanvasGroup, true, 0.5f);
            yield return context.WaitOrSkip(3.0f);
            yield return context.FadeCanvasGroup(context._winLoseBannerCanvasGroup, false, 0.5f);

            // Step 2: Show Main Stats Panel and Detailed Stats
            yield return context.FadeCanvasGroup(context._statsPanelCanvasGroup, true, 0.5f);
            yield return context.FadeCanvasGroup(context._detailsSubPanelCanvasGroup, true, 0.5f);
            yield return context.WaitOrSkip(5.0f);

            // Step 3: Show Action Buttons
            yield return context.FadeCanvasGroup(context._actionsPanelCanvasGroup, true, 0.5f);
            context.SetSkipButtonActive(false);
        }
    }
}
