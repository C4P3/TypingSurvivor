using System.Collections.Generic;

// MonoBehaviourを継承しないプレーンなC#クラスに変更
public class PlayerStatusSystem : IPlayerStatusSystemWriter, IPlayerStatusSystemReader
{
    // ulong: clientId, PlayerStat: 強化対象, float: 強化値の合計
    private readonly Dictionary<ulong, Dictionary<PlayerStat, float>> _permanentStats = new();

    public void AddPermanentModifier(ulong clientId, PlayerStat stat, float value)
    {
        if (!_permanentStats.ContainsKey(clientId))
        {
            _permanentStats[clientId] = new Dictionary<PlayerStat, float>();
        }

        // 既存の値に加算
        _permanentStats[clientId][stat] = _permanentStats[clientId].GetValueOrDefault(stat) + value;
        
        // TODO: 将来的には、ここでセーブデータへの書き込みをトリガーする
    }

    public float GetStatValue(ulong clientId, PlayerStat stat)
    {
        float baseValue = GetBaseStatValue(stat);
        float permanentBonus = _permanentStats.GetValueOrDefault(clientId)?.GetValueOrDefault(stat) ?? 0f;
        
        // TODO: 将来的には、アイテムなどによる一時的な効果もここで合算する
        return baseValue + permanentBonus;
    }

    private float GetBaseStatValue(PlayerStat stat)
    {
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