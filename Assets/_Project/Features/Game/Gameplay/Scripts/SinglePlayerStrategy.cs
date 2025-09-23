using TypingSurvivor.Features.Game.Gameplay.Data;

namespace TypingSurvivor.Features.Game.Gameplay
{
    public class SinglePlayerStrategy : IGameModeStrategy
    {
        public int PlayerCount => 1;

        public bool IsGameOver(IGameStateReader gameState)
        {
            // 自分の酸素が0になったらゲームオーバー
            return gameState.CurrentOxygen <= 0;
        }
        // TODO: 実装
        public GameResult CalculateResult(IGameStateReader gameState)
        {
            return new GameResult();
        }
    }
}