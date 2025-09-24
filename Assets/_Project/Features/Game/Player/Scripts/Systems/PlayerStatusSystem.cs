using System.Collections.Generic;
using UnityEngine;

public class PlayerStatusSystem : MonoBehaviour, IPlayerStatusSystemWriter, IPlayerStatusSystemReader
{
    // TODO: 本来はプレイヤーごとの永続データを管理する
    private Dictionary<ulong, Dictionary<PlayerStat, float>> _playerStats = new();

    public void AddPermanentModifier(ulong userId, PlayerStat stat, float value)
    {
        // 仮実装: 今は特に何もしない
        Debug.LogWarning("AddPermanentModifier is not fully implemented yet.");
    }

    public float GetStatValue(ulong userId, PlayerStat stat)
    {
        // 仮実装: 固定値を返す
        switch (stat)
        {
            case PlayerStat.MoveSpeed:
                return 4.0f; // 1秒間に4タイル移動する速さ
            case PlayerStat.MaxOxygen:
                return 100f;
            case PlayerStat.RadarRange:
                return 5f;
            default:
                return 1.0f;
        }
    }
}