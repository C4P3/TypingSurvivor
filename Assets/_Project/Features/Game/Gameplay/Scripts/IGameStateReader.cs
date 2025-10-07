using TypingSurvivor.Features.Game.Gameplay.Data;
using Unity.Netcode;

/// <summary>
/// ゲーム状態の読み取り専用インターフェース。UIなど表示系が使用する。
/// </summary>
public interface IGameStateReader
{
    NetworkVariable<GamePhase> CurrentPhaseNV { get; }
    NetworkVariable<float> GameTimer { get; }
    NetworkList<PlayerData> PlayerDatas { get; }
    NetworkList<NetworkObjectReference> SpawnedPlayers { get; }
    float CurrentOxygen { get; } // This might become obsolete or represent local player's oxygen
    event System.Action<ulong, float> OnOxygenChanged;
}