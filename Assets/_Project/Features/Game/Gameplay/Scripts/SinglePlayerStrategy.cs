using TypingSurvivor.Features.Game.Gameplay.Data;

namespace TypingSurvivor.Features.Game.Gameplay
{
    public class SinglePlayerStrategy : IGameModeStrategy
    {
        public int PlayerCount => 1;

        public bool IsGameOver(IGameStateReader gameState)
        {
            // The game ends when the single player is game over.
            if (gameState.PlayerDatas.Count > 0)
            {
                return gameState.PlayerDatas[0].IsGameOver;
            }
            // If no player data exists, the game can't continue.
            return true;
        }
        // TODO: 実装
        public GameResult CalculateResult(IGameStateReader gameState)
        {
            return new GameResult();
        }
    }
}