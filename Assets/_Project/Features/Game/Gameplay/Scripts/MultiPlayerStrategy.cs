using System.Collections.Generic;
using System.Linq;
using TypingSurvivor.Features.Game.Gameplay.Data;

namespace TypingSurvivor.Features.Game.Gameplay
{
    public class MultiPlayerStrategy : IGameModeStrategy
    {
        public int PlayerCount => 2;

        public bool IsGameOver(IGameStateReader gameState)
        {
            int aliveCount = 0;
            foreach (var player in gameState.PlayerDatas)
            {
                if (player.Oxygen > 0)
                {
                    aliveCount++;
                }
            }
            return aliveCount <= 1;
        }

        public GameResult CalculateResult(IGameStateReader gameState)
        {
            int aliveCount = 0;
            ulong winnerId = 0;
            foreach (var player in gameState.PlayerDatas)
            {
                if (player.Oxygen > 0)
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
                    WinnerClientId = 0, // Using 0 for winnerId in a draw
                    FinalPlayerDatas = finalPlayerDatas
                };
            }
        }
    }
}
