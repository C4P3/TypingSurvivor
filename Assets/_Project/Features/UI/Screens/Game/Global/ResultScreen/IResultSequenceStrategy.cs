using System.Collections;
using static TypingSurvivor.Features.Game.Gameplay.GameManager;

namespace TypingSurvivor.Features.UI.Screens
{
    /// <summary>
    /// Defines the contract for a strategy that controls the sequence of animations on the result screen.
    /// </summary>
    public interface IResultSequenceStrategy
    {
        /// <summary>
        /// Executes the result screen animation sequence.
        /// </summary>
        /// <param name="context">The ResultScreen instance, used to access UI elements and helper methods.</param>
        /// <param name="dto">The data transfer object containing all result information.</param>
        /// <param name="personalBest">The player's personal best time (for single player).</param>
        /// <param name="playerRank">The player's rank (for single player).</param>
        /// <param name="totalPlayers">The total players on the leaderboard (for single player).</param>
        /// <returns>An IEnumerator to be run as a coroutine.</returns>
        IEnumerator ExecuteSequence(ResultScreen context, GameResultDto dto, float personalBest, int playerRank, int totalPlayers);
    }
}
