namespace TypingSurvivor.Features.Game.Gameplay.Data
{
    // リザルト情報のデータ構造
    public struct GameResult
    {
        public bool IsDraw; // trueの場合、引き分け
        public ulong WinnerClientId; // 勝者のID (IsDrawがtrueの場合は0)
        // 他にも最終スコアなど
    }
}