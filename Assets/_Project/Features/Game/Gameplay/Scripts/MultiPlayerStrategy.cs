using TypingSurvivor.Features.Game.Gameplay.Data;

namespace TypingSurvivor.Features.Game.Gameplay
{
    public class MultiPlayerStrategy : IGameModeStrategy
    {
        public int PlayerCount => 2;
        public bool IsGameOver(IGameStateReader gameState)
        {
            int alivePlayers = 0;
            foreach (var playerData in gameState.PlayerDatas)
            {
                if (!playerData.IsGameOver)
                {
                    alivePlayers++;
                }
            }

            // For a standard multiplayer game (2+ players), the game ends when 1 or 0 players are left.
            // For a game that might have started with 1 player (e.g. debug), it ends when 0 are left.
            if (gameState.PlayerDatas.Count < 2)
            {
                return alivePlayers == 0;
            }
            
            return alivePlayers <= 1;
        }
        // ... CalculateResultの実装 ...
        // TODO: 実装
        public GameResult CalculateResult(IGameStateReader gameState)
        {
            return new GameResult();
        }
    }
}