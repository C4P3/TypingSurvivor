using System.Collections;
using TypingSurvivor.Features.Game.Level.Data;
using TypingSurvivor.Features.Game.Settings;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using TypingSurvivor.Features.Game.Level;
using TypingSurvivor.Features.Core.PlayerStatus;
using TypingSurvivor.Features.Game.Player;
using TypingSurvivor.Features.Game.Gameplay.Data;

namespace TypingSurvivor.Features.Game.Gameplay
{
    public class GameManager : NetworkBehaviour, IGameStateWriter
    {
        private GameState _gameState;
        private IGameModeStrategy _gameModeStrategy;
        private ILevelService _levelService;
        private IPlayerStatusSystemReader _statusReader;
        private IPlayerStatusSystemWriter _statusWriter;
        private Grid _grid;
        private readonly Dictionary<ulong, PlayerFacade> _playerInstances = new();
        private readonly HashSet<ulong> _rematchRequesters = new();
        private Coroutine _serverGameLoop;
        private GameConfig _gameConfig;
        private float oxygenDecreaseRate = 1.0f;

        public void Initialize(GameState gameState, IGameModeStrategy gameModeStrategy, ILevelService levelService, IPlayerStatusSystemReader statusReader, IPlayerStatusSystemWriter statusWriter, GameConfig gameConfig, Grid grid)
        {
            _gameState = gameState;
            _gameModeStrategy = gameModeStrategy;
            _levelService = levelService;
            _statusReader = statusReader;
            _statusWriter = statusWriter;
            _gameConfig = gameConfig;
            _grid = grid;
        }

        public override void OnNetworkSpawn()
        {
            if (IsClient)
            {
                _gameState.CurrentPhase.OnValueChanged += HandlePhaseChanged_Client;
            }
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
                
                // Initialize players already connected
                foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    HandleClientConnected(clientId);
                }

                _serverGameLoop = StartCoroutine(ServerGameLoop());
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsClient && _gameState != null)
            {
                _gameState.CurrentPhase.OnValueChanged -= HandlePhaseChanged_Client;
            }
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (!IsServer) return;
            // PlayerDatasリストを初期化
            _gameState.PlayerDatas.Add(new PlayerData { ClientId = clientId, Score = 0, Oxygen = 100f, IsGameOver = false });
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (!IsServer) return;

            // Netcode automatically despawns the player object. We just need to clean up our game state.
            if (_playerInstances.TryGetValue(clientId, out var playerFacade))
            {
                // Remove from the synced list of spawned players
                for (int i = 0; i < _gameState.SpawnedPlayers.Count; i++)
                {
                    if (_gameState.SpawnedPlayers[i].TryGet(out var networkObject) && networkObject == playerFacade.NetworkObject)
                    {
                        _gameState.SpawnedPlayers.RemoveAt(i);
                        break;
                    }
                }
            }

            // Clean up server-side tracking
            _playerInstances.Remove(clientId);

            // Clean up GameState data
            for (int i = _gameState.PlayerDatas.Count - 1; i >= 0; i--)
            {
                if (_gameState.PlayerDatas[i].ClientId == clientId)
                {
                    _gameState.PlayerDatas.RemoveAt(i);
                    break;
                }
            }

            // --- Re-evaluate Game State based on the current phase ---
            var currentPhase = _gameState.CurrentPhase.Value;

            // Case 1: Did the game end because a player disconnected during gameplay?
            if (currentPhase == GamePhase.Playing && _gameModeStrategy.IsGameOver(_gameState))
            {
                _gameState.CurrentPhase.Value = GamePhase.Finished;
                return;
            }

            // Case 2: Did the prerequisites for starting a game break?
            if (currentPhase == GamePhase.WaitingForPlayers || currentPhase == GamePhase.Countdown)
            {
                if (_playerInstances.Count < _gameModeStrategy.PlayerCount)
                {
                    Debug.Log("A player disconnected before the game started. Aborting and moving to results.");
                    _gameState.CurrentPhase.Value = GamePhase.Finished;
                }
            }
        }

        private IEnumerator ServerGameLoop()
        {
            // --- Setup Phase (occurs only once) ---
            yield return StartCoroutine(WaitingForPlayersPhase());
            yield return StartCoroutine(InitialSpawnPhase());

            // --- Game Round Loop (repeats for rematches) ---
            while (true)
            {
                yield return StartCoroutine(CountdownPhase());
                yield return StartCoroutine(PlayingPhase());
                yield return StartCoroutine(FinishedPhase());
            }
        }

        private IEnumerator WaitingForPlayersPhase()
        {
            _gameState.CurrentPhase.Value = GamePhase.WaitingForPlayers;
            while (_gameState.PlayerDatas.Count < _gameModeStrategy.PlayerCount)
            {
                yield return null;
            }
        }

        private IEnumerator CountdownPhase()
        {
            _gameState.CurrentPhase.Value = GamePhase.Countdown;
            yield return new WaitForSeconds(5); // Countdown duration
        }

        private IEnumerator PlayingPhase()
        {
            _gameState.CurrentPhase.Value = GamePhase.Playing;
            while (!_gameModeStrategy.IsGameOver(_gameState))
            {
                // Decrease oxygen for all players
                for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
                {
                    var data = _gameState.PlayerDatas[i];
                    if (data.IsGameOver) continue;

                    // Get the current damage reduction for the player
                    float damageReduction = _statusReader.GetStatValue(data.ClientId, PlayerStat.DamageReduction);
                    damageReduction = Mathf.Clamp01(damageReduction); // Ensure it's between 0 and 1

                    // Calculate the actual oxygen decrease
                    float actualDecrease = oxygenDecreaseRate * (1.0f - damageReduction);
                    data.Oxygen -= actualDecrease * Time.deltaTime;

                    if (data.Oxygen <= 0)
                    {
                        data.Oxygen = 0;
                        data.IsGameOver = true;
                    }
                    _gameState.PlayerDatas[i] = data;
                }
                yield return null;
            }
        }

        private IEnumerator InitialSpawnPhase()
        {
            // --- Pre-flight checks for configuration ---
            if (_gameConfig.DefaultMapGenerator == null || _gameConfig.VersusSpawnStrategy == null || _gameConfig.DefaultItemPlacementStrategy == null)
            {
                Debug.LogError("GameConfig is missing one or more required assets (DefaultMapGenerator, VersusSpawnStrategy, or DefaultItemPlacementStrategy). Aborting spawn.");
                yield break;
            }

            // 1. Build the map generation request based on the game mode
            var request = new MapGenerationRequest();
            var clientIds = NetworkManager.Singleton.ConnectedClientsIds.ToList();

            // For now, use a simple logic. This can be expanded for different modes.
            if (_gameModeStrategy is MultiPlayerStrategy)
            {
                for (int i = 0; i < clientIds.Count; i++)
                {
                    request.SpawnAreas.Add(new SpawnArea
                    {
                        PlayerClientIds = new List<ulong> { clientIds[i] },
                        WorldOffset = new Vector2Int(i * 1000, 0),
                        MapGenerator = _gameConfig.DefaultMapGenerator as IMapGenerator,
                        SpawnPointStrategy = _gameConfig.VersusSpawnStrategy as ISpawnPointStrategy
                    });
                }
            }
            else // SinglePlayer
            {
                request.SpawnAreas.Add(new SpawnArea
                {
                    PlayerClientIds = new List<ulong> { clientIds[0] },
                    WorldOffset = new Vector2Int(0, 0),
                    MapGenerator = _gameConfig.DefaultMapGenerator as IMapGenerator,
                    SpawnPointStrategy = _gameConfig.SinglePlayerSpawnStrategy as ISpawnPointStrategy
                });
            }

            // 2. Tell LevelManager to build the world
            _levelService.GenerateWorld(request);
            yield return null; // Give LevelManager a frame to process

            // 3. Spawn players in their designated areas
            foreach (var area in request.SpawnAreas)
            {
                var spawnPoints = _levelService.GetSpawnPoints(area);
                for (int i = 0; i < area.PlayerClientIds.Count; i++)
                {
                    ulong clientId = area.PlayerClientIds[i];
                    Vector3Int gridPos = spawnPoints[i];
                    Vector3 spawnPos = _grid.GetCellCenterWorld(gridPos);

                    GameObject playerInstance = Instantiate(_gameConfig.PlayerPrefab, spawnPos, Quaternion.identity);
                    var playerNetworkObject = playerInstance.GetComponent<NetworkObject>();
                    playerNetworkObject.SpawnAsPlayerObject(clientId, true);

                    var playerFacade = playerInstance.GetComponent<TypingSurvivor.Features.Game.Player.PlayerFacade>();
                    playerFacade.NetworkGridPosition.Value = gridPos;

                    // Register initial position in the GameState
                    UpdatePlayerPosition(clientId, gridPos);

                    _playerInstances[clientId] = playerFacade;
                    _gameState.SpawnedPlayers.Add(playerNetworkObject);
                }
            }
        }

        private IEnumerator FinishedPhase()
        {
            _gameState.CurrentPhase.Value = GamePhase.Finished;
            _rematchRequesters.Clear();

            // This loop will naturally terminate if all players request a rematch OR if a player disconnects
            // (because _playerInstances.Count will decrease).
            while (_rematchRequesters.Count < _playerInstances.Count)
            {
                yield return null;
            }

            // FINAL GATEKEEPER: After the loop, check if we have enough players to start a rematch.
            // This prevents a rematch if the loop was exited due to a disconnection.
            if (_playerInstances.Count < _gameModeStrategy.PlayerCount)
            {
                Debug.Log("A player disconnected while waiting for rematch. Rematch is cancelled.");
                // Wait indefinitely on the results screen.
                while (true)
                {
                    yield return null;
                }
            }
            
            // If we passed the check, it means everyone requested a rematch. Proceed.
            // --- Prepare for the next round ---
            ResetPlayersForRematch();

            // Regenerate world using the same request logic as initial spawn
            var request = new MapGenerationRequest();
            var clientIds = _playerInstances.Keys.ToList();

            if (_gameModeStrategy is MultiPlayerStrategy)
            {
                for (int i = 0; i < clientIds.Count; i++)
                {
                    request.SpawnAreas.Add(new SpawnArea
                    {
                        PlayerClientIds = new List<ulong> { clientIds[i] },
                        WorldOffset = new Vector2Int(i * 1000, 0),
                        MapGenerator = _gameConfig.DefaultMapGenerator as IMapGenerator,
                        SpawnPointStrategy = _gameConfig.VersusSpawnStrategy as ISpawnPointStrategy
                    });
                }
            }
            else
            {
                 request.SpawnAreas.Add(new SpawnArea
                {
                    PlayerClientIds = new List<ulong> { clientIds[0] },
                    WorldOffset = new Vector2Int(0, 0),
                    MapGenerator = _gameConfig.DefaultMapGenerator as IMapGenerator,
                    SpawnPointStrategy = _gameConfig.SinglePlayerSpawnStrategy as ISpawnPointStrategy
                });
            }
            _levelService.GenerateWorld(request);
            yield return null;

            // Reposition players
            foreach (var area in request.SpawnAreas)
            {
                var spawnPoints = _levelService.GetSpawnPoints(area);
                for (int i = 0; i < area.PlayerClientIds.Count; i++)
                {
                    ulong clientId = area.PlayerClientIds[i];
                    var player = _playerInstances[clientId];
                    var gridPos = spawnPoints[i];
                    _levelService.ClearArea(gridPos, 1);
                    var spawnPos = _grid.GetCellCenterWorld(gridPos);
                    player.RespawnAt(spawnPos);
                    // After teleporting the player, update their position in the GameState and force a chunk update.
                    UpdatePlayerPosition(clientId, gridPos);
                    _levelService.ForceChunkUpdateForPlayer(clientId, spawnPos);
                }
            }
        }

        public void ResetPlayersForRematch()
        {
            if (!IsServer) return;

            for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
            {
                var data = _gameState.PlayerDatas[i];
                
                // Clear temporary buffs from the previous session
                _statusWriter.ClearSessionModifiers(data.ClientId);
                
                // Get the (potentially modified) max oxygen for this player
                float maxOxygen = _statusReader.GetStatValue(data.ClientId, PlayerStat.MaxOxygen);

                // Reset runtime stats
                data.Score = 0;
                data.IsGameOver = false;
                data.Oxygen = maxOxygen;
                
                _gameState.PlayerDatas[i] = data;
            }
        }

        // --- IGameStateWriter Implementation ---
        public void AddOxygen(ulong clientId, float amount)
        {
            if (!IsServer) return;
            for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
            {
                if (_gameState.PlayerDatas[i].ClientId == clientId)
                {
                    var data = _gameState.PlayerDatas[i];
                    float maxOxygen = _statusReader.GetStatValue(clientId, PlayerStat.MaxOxygen);
                    data.Oxygen = Mathf.Clamp(data.Oxygen + amount, 0, maxOxygen);
                    _gameState.PlayerDatas[i] = data;
                    return;
                }
            }
        }

        public void AddScore(ulong clientId, int amount)
        {
            if (!IsServer) return;
            for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
            {
                if (_gameState.PlayerDatas[i].ClientId == clientId)
                {
                    var data = _gameState.PlayerDatas[i];
                    data.Score += amount;
                    _gameState.PlayerDatas[i] = data;
                    return;
                }
            }
        }
        public void UpdatePlayerPosition(ulong clientId, Vector3Int gridPosition)
        {
            if (!IsServer) return;
            for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
            {
                if (_gameState.PlayerDatas[i].ClientId == clientId)
                {
                    var data = _gameState.PlayerDatas[i];
                    data.GridPosition = gridPosition;
                    _gameState.PlayerDatas[i] = data;
                    return;
                }
            }
        }
        public void SetPlayerGameOver(ulong clientId)
        {
            if (!IsServer) return;
            for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
            {
                if (_gameState.PlayerDatas[i].ClientId == clientId)
                {
                    var data = _gameState.PlayerDatas[i];
                    data.IsGameOver = true;
                    _gameState.PlayerDatas[i] = data;
                    return;
                }
            }
        }
        
        private void HandlePhaseChanged_Client(GamePhase previousPhase, GamePhase newPhase)
        {
            Debug.Log($"Game phase changed to: {newPhase}");
        }

        // --- Rematch Logic ---
        [ServerRpc(RequireOwnership = false)]
        public void RequestRematchServerRpc(ServerRpcParams rpcParams = default)
        {
            if (_gameState.CurrentPhase.Value != GamePhase.Finished) return;

            ulong clientId = rpcParams.Receive.SenderClientId;
            if (_playerInstances.ContainsKey(clientId) && !_rematchRequesters.Contains(clientId))
            {
                _rematchRequesters.Add(clientId);
                Debug.Log($"Player {clientId} requested a rematch. {_rematchRequesters.Count}/{_playerInstances.Count}");
            }
        }
    }
}