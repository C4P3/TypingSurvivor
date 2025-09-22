public enum PlayerStat { MoveSpeed, MaxHealth, RadarRange }

/// <summary>
/// プレイヤーの永続的なステータス（ローグライク要素）を管理するシステムのインターフェース
/// </summary>
public interface IPlayerStatusSystem
{
    void AddPermanentModifier(ulong userId, PlayerStat stat, float value);
    float GetStatValue(ulong userId, PlayerStat stat);
}