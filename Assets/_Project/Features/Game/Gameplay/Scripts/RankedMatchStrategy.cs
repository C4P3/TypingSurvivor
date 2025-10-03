using TypingSurvivor.Features.Game.Gameplay.Data;
using System.Linq;

namespace TypingSurvivor.Features.Game.Gameplay
{
    public class RankedMatchStrategy : IGameModeStrategy
    {
        public int PlayerCount => 2;

        public bool IsGameOver(IGameStateReader gameState)
        {
            // Game is over if only one or fewer players are left with oxygen > 0.
            return gameState.PlayerDatas.Count(p => p.CurrentOxygen > 0) <= 1;
        }

        public GameResult CalculateResult(IGameStateReader gameState)
        {
            // This strategy's responsibility is to determine the outcome based on the game state.
            // The actual rating calculation will be handled by a separate system.

            var alivePlayers = gameState.PlayerDatas.Where(p => p.CurrentOxygen > 0).ToList();

            if (alivePlayers.Count == 1)
            {
                // A single winner
                return new GameResult
                {
                    IsDraw = false,
                    WinnerClientId = alivePlayers[0].ClientId
                };
            }
            else
            {
                // A draw (e.g., both players run out of oxygen at the same time) or no players.
                return new GameResult
                {
                    IsDraw = true,
                    WinnerClientId = 0
                };
            }
        }
    }
}
