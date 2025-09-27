/// <summary>
/// ゲーム状態の書き込み専用インターフェース。アイテム効果など、状態を変更する権限を持つクラスが使用する。
/// </summary>
public interface IGameStateWriter
{
    void AddScore(ulong clientId, int amount);
    void SetPlayerGameOver(ulong clientId);
    void AddOxygen(ulong clientId, float amount);
    void ResetPlayersForRematch();
}