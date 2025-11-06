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
        public NetworkVariable<int> RematchRequesterCount { get; } = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> OxygenLevel { get; } = new(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkList<PlayerData> PlayerDatas { get; } = new();
        private readonly NetworkList<NetworkObjectReference> _spawnedPlayers = new();

        // --- IGameStateReader Implementation ---
        public NetworkVariable<GamePhase> CurrentPhaseNV => CurrentPhase;
        public NetworkVariable<float> RematchTimerRemainingNV => RematchTimerRemaining;
        public NetworkVariable<int> RematchRequesterCountNV => RematchRequesterCount;
        public NetworkList<NetworkObjectReference> SpawnedPlayers => _spawnedPlayers;
        public float CurrentOxygen => OxygenLevel.Value; // Kept for single player logic for now
    }
}
