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
        public NetworkVariable<float> RematchTimerRemaining { get; } = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> OxygenLevel { get; } = new(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkList<PlayerData> PlayerDatas { get; } = new();
        private readonly NetworkList<NetworkObjectReference> _spawnedPlayers = new();

        // --- IGameStateReader Implementation ---
        public NetworkVariable<GamePhase> CurrentPhaseNV => CurrentPhase;
        public NetworkVariable<float> RematchTimerRemainingNV => RematchTimerRemaining;
        public NetworkList<NetworkObjectReference> SpawnedPlayers => _spawnedPlayers;
        public float CurrentOxygen => OxygenLevel.Value; // Kept for single player logic for now
        public event Action<ulong, float> OnOxygenChanged;

        public override void OnNetworkSpawn()
        {
            _spawnedPlayers.OnListChanged += HandleSpawnedPlayersChanged;
            PlayerDatas.OnListChanged += HandlePlayerDatasChanged;
        }

        public override void OnNetworkDespawn()
        {
            _spawnedPlayers.OnListChanged -= HandleSpawnedPlayersChanged;
            PlayerDatas.OnListChanged -= HandlePlayerDatasChanged;
        }

        private void HandleSpawnedPlayersChanged(NetworkListEvent<NetworkObjectReference> changeEvent)
        {
            // This is just a trigger for systems like CameraManager to refresh.
            // The handler itself doesn't need to do anything.
        }

        private void HandlePlayerDatasChanged(NetworkListEvent<PlayerData> changeEvent)
        {
            PlayerData changedData;

            switch (changeEvent.Type)
            {
                case NetworkListEvent<PlayerData>.EventType.Add:
                    changedData = PlayerDatas[changeEvent.Index];
                    break;
                case NetworkListEvent<PlayerData>.EventType.Value:
                    changedData = changeEvent.Value;
                    break;
                default:
                    // For other events like Clear, Remove, etc., we don't need to fire individual updates.
                    return;
            }

            // Notify listeners about the specific player data that changed
            OnOxygenChanged?.Invoke(changedData.ClientId, changedData.Oxygen);
        }
    }
}
