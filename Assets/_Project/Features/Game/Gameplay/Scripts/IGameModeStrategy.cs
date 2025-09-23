using TypingSurvivor.Features.Game.Gameplay.Data;

namespace TypingSurvivor.Features.Game.Gameplay
{
    public interface IGameModeStrategy
    {
        int PlayerCount { get; }
        bool IsGameOver(IGameStateReader gameState);

        // ゲーム終了時に、勝者や最終スコアなどのリザルト情報を計算する
        GameResult CalculateResult(IGameStateReader gameState);
    }
}