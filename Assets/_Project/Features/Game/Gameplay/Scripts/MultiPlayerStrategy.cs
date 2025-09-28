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

            // A multiplayer game is over when only one or zero players are left alive.
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