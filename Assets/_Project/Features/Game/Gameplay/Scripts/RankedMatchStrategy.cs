using System.Collections.Generic;
using System.Linq;
using TypingSurvivor.Features.Game.Gameplay.Data;

namespace TypingSurvivor.Features.Game.Gameplay
{
    public class RankedMatchStrategy : IGameModeStrategy
    {
        public int PlayerCount => 2;

        public bool IsGameOver(IGameStateReader gameState)
        {
            // Game is over if only one or fewer players are not in a GameOver state.
            int aliveCount = 0;
            foreach (var player in gameState.PlayerDatas)
            {
                if (!player.IsGameOver)
                {
                    aliveCount++;
                }
            }
            return aliveCount <= 1;
        }

        public GameResult CalculateResult(IGameStateReader gameState)
        {
            // This strategy's responsibility is to determine the outcome based on the game state.
            // The actual rating calculation will be handled by a separate system.

            int aliveCount = 0;
            ulong winnerId = 0;
            foreach (var player in gameState.PlayerDatas)
            {
                if (!player.IsGameOver)
                {
                    aliveCount++;
                    winnerId = player.ClientId;
                }
            }

            List<PlayerData> finalPlayerDatas = new List<PlayerData>();
            for (int i = 0; i < gameState.PlayerDatas.Count; i++)
            {
                finalPlayerDatas.Add(gameState.PlayerDatas[i]);
            }

            if (aliveCount == 1)
            {
                // A single winner
                return new GameResult
                {
                    IsDraw = false,
                    WinnerClientId = winnerId,
                    FinalPlayerDatas = finalPlayerDatas
                };
            }
            else
            {
                // A draw (e.g., both players run out of oxygen at the same time) or no players.
                return new GameResult
                {
                    IsDraw = true,
                    WinnerClientId = 0,
                    FinalPlayerDatas = finalPlayerDatas
                };
            }
        }
    }
}