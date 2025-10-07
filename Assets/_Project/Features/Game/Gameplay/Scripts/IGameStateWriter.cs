using UnityEngine;

/// <summary>
/// ゲーム状態の書き込み専用インターフェース。アイテム効果など、状態を変更する権限を持つクラスが使用する。
/// </summary>
public interface IGameStateWriter
{
    void SetPlayerGameOver(ulong clientId);
    void AddOxygen(ulong clientId, float amount);
    void UpdatePlayerPosition(ulong clientId, Vector3Int gridPosition);
    void UpdatePlayerName(ulong clientId, string playerName);
    void AddBlocksDestroyed(ulong clientId, int amount);
    void AddTypingMisses(ulong clientId, int amount);
    void AddTypingTime(ulong clientId, float time);
    void AddCharsTyped(ulong clientId, int charCount);
    void AddKeyPresses(ulong clientId, int pressCount);
    void ResetPlayersForRematch();
}