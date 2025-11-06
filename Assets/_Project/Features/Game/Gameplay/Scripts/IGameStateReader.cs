using TypingSurvivor.Features.Game.Gameplay.Data;
using Unity.Netcode;

/// <summary>
/// ゲーム状態の読み取り専用インターフェース。UIなど表示系が使用する。
/// </summary>
public interface IGameStateReader
{
    NetworkVariable<GamePhase> CurrentPhaseNV { get; }
    NetworkVariable<float> GameTimer { get; }
    NetworkVariable<float> RematchTimerRemainingNV { get; }
    NetworkVariable<int> RematchRequesterCountNV { get; }
    NetworkList<PlayerData> PlayerDatas { get; }
    NetworkList<NetworkObjectReference> SpawnedPlayers { get; }
}