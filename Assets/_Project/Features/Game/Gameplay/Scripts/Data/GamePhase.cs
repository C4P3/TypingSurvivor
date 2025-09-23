namespace TypingSurvivor.Features.Game.Gameplay.Data
{
    public enum GamePhase
    {
        WaitingForPlayers, // プレイヤーの接続待ち（マルチプレイ用）
        Countdown,         // ゲーム開始前のカウントダウン
        Playing,           // ゲームプレイ中
        Finished,          // ゲーム終了（リザルト表示へ）
        Paused             // 一時停止中
    }
}