using TypingSurvivor.Features.Game.Gameplay.Data;

namespace TypingSurvivor.Features.Game.Gameplay
{
    public class MultiPlayerStrategy : IGameModeStrategy
    {
        public int PlayerCount => 2;
        public bool IsGameOver(IGameStateReader gameState)
        {
            // いずれかのプレイヤーがゲームオーバーになったら終了
            foreach (var playerData in gameState.PlayerDatas)
            {
                if (playerData.IsGameOver) return true;
            }
            return false;
        }
        // ... CalculateResultの実装 ...
        // TODO: 実装
        public GameResult CalculateResult(IGameStateReader gameState)
        {
            return new GameResult();
        }
    }
}