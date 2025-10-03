using System.Threading.Tasks;
using TypingSurvivor.Features.Game.Gameplay.Data;

namespace TypingSurvivor.Features.Game.Rating
{
    public interface IRatingService
    {
        /// <summary>
        /// Handles the game finished event to calculate and update player ratings.
        /// </summary>
        /// <param name="result">The result of the game.</param>
        Task HandleGameFinished(GameResult result);
    }
}
