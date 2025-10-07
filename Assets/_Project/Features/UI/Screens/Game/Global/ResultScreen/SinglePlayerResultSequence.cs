using System.Collections;
using static TypingSurvivor.Features.Game.Gameplay.GameManager;

namespace TypingSurvivor.Features.UI.Screens
{
    public class SinglePlayerResultSequence : IResultSequenceStrategy
    {
        public IEnumerator ExecuteSequence(ResultScreen context, GameResultDto dto, float personalBest, int playerRank, int totalPlayers)
        {
            context.HideAllCanvasGroups();
            context.SetSkipButtonActive(true);

            // Step 1: Show "GAME OVER" Banner
            yield return context.FadeCanvasGroup(context._winLoseBannerCanvasGroup, true, 0.5f);
            yield return context.WaitOrSkip(3.0f);
            yield return context.FadeCanvasGroup(context._winLoseBannerCanvasGroup, false, 0.5f);

            // Step 2: Show Main Stats Panel and basic info
            yield return context.FadeCanvasGroup(context._statsPanelCanvasGroup, true, 0.5f);
            yield return context.FadeCanvasGroup(context._basicStatsCanvasGroup, true, 0.5f);
            yield return context.WaitOrSkip(3.0f);

            // Step 3: Show "New Record!" if applicable
            bool isNewRecord = dto.FinalGameTime > personalBest && personalBest > 0;
            if (isNewRecord)
            {
                yield return context.FadeCanvasGroup(context._newRecordCanvasGroup, true, 0.5f);
                yield return context.WaitOrSkip(2.0f);
            }

            // Step 4: Show Rank info if applicable
            bool hasRank = playerRank > 0 && totalPlayers > 0;
            if (hasRank)
            {
                yield return context.FadeCanvasGroup(context._rankCanvasGroup, true, 0.5f);
                yield return context.WaitOrSkip(3.0f);
            }

            // Step 5: Show Detailed Stats
            yield return context.FadeCanvasGroup(context._detailsSubPanelCanvasGroup, true, 0.5f);
            yield return context.WaitOrSkip(5.0f);

            // Step 6: Show Action Buttons
            yield return context.FadeCanvasGroup(context._actionsPanelCanvasGroup, true, 0.5f);
            context.SetSkipButtonActive(false);
        }
    }
}
