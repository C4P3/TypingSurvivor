/// <summary>
/// タイピング機能を開始するためのサービスインターフェース。
/// </summary>
public interface ITypingService
{
    // 特定のプレイヤーのためにタイピングセッションを開始する。
    // 成功/失敗の結果はイベントやコールバックで返す。
    void StartTypingSession(ulong clientId, TypingChallenge challenge);
}