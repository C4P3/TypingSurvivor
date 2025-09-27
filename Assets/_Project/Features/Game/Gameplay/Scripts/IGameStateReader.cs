using TypingSurvivor.Features.Game.Gameplay.Data;
using Unity.Netcode;

/// <summary>
/// ゲーム状態の読み取り専用インターフェース。UIなど表示系が使用する。
/// </summary>
public interface IGameStateReader
{
    NetworkVariable<GamePhase> CurrentPhaseNV { get; }
    NetworkList<PlayerData> PlayerDatas { get; }
    float CurrentOxygen { get; } // This might become obsolete or represent local player's oxygen
    int GetPlayerScore(ulong clientId);
    event System.Action<ulong, float> OnOxygenChanged;
    event System.Action<int> OnScoreChanged;
}