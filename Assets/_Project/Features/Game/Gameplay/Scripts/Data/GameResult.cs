namespace TypingSurvivor.Features.Game.Gameplay.Data
{
    // リザルト情報のデータ構造
    public struct GameResult
    {
        public ulong WinnerClientId; // 勝者のID（-1なら引き分けなど）
        // 他にも最終スコアなど
    }
}