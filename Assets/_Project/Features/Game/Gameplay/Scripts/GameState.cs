using Unity.Netcode;
using TypingSurvivor.Features.Game.Gameplay.Data;
using System;

namespace TypingSurvivor.Features.Game.Gameplay
{
    /// <summary>
    /// Holds all the NetworkVariables that represent the current state of the game.
    /// Implements the IGameStateReader interface to provide read-only access to other systems.
    /// </summary>
    public class GameState : NetworkBehaviour, IGameStateReader
    {
        public NetworkVariable<GamePhase> CurrentPhase { get; } = new(GamePhase.WaitingForPlayers);
        public NetworkVariable<float> GameTimer { get; } = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> OxygenLevel { get; } = new(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkList<PlayerData> PlayerDatas { get; } = new();

        // --- IGameStateReader Implementation ---
        public NetworkVariable<GamePhase> CurrentPhaseNV => CurrentPhase;
        public float CurrentOxygen => OxygenLevel.Value;
        public event Action<float> OnOxygenChanged;
        public event Action<int> OnScoreChanged; // TODO: NetworkListの変更を検知して発火させる仕組みが必要

        public override void OnNetworkSpawn()
        {
            // NetworkVariableの値が変更されたときにイベントを発火させる
            OxygenLevel.OnValueChanged += (previousValue, newValue) => OnOxygenChanged?.Invoke(newValue);
            PlayerDatas.OnListChanged += HandlePlayerDatasChanged;
        }

        public override void OnNetworkDespawn()
        {
            // 忘れずに購読解除
            OxygenLevel.OnValueChanged -= (previousValue, newValue) => OnOxygenChanged?.Invoke(newValue);
            PlayerDatas.OnListChanged -= HandlePlayerDatasChanged;
        }

        private void HandlePlayerDatasChanged(NetworkListEvent<PlayerData> changeEvent)
        {
            // とりあえず、リストの何かが変わったら全プレイヤーのスコア更新イベントを飛ばす
            // TODO: より効率的な方法を検討
            if (NetworkManager.Singleton.IsClient)
            {
                var localPlayerId = NetworkManager.Singleton.LocalClientId;
                OnScoreChanged?.Invoke(GetPlayerScore(localPlayerId));
            }
        }

        public int GetPlayerScore(ulong clientId)
        {
            foreach (var playerData in PlayerDatas)
            {
                if (playerData.ClientId == clientId)
                {
                    return playerData.Score;
                }
            }
            return 0;
        }
    }
}
