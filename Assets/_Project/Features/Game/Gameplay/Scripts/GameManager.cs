using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TypingSurvivor.Features.Game.Gameplay.Data;
using TypingSurvivor.Features.Game.Settings;
using TypingSurvivor.Features.Game.Level;
using TypingSurvivor.Features.Game.Player;
using TypingSurvivor.Features.Core.PlayerStatus; // ILevelServiceのために追加

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

        [SerializeField] private GameConfig _gameConfig;
        private float oxygenDecreaseRate = 0.9f;

        public void Initialize(GameState gameState, IGameModeStrategy gameModeStrategy, ILevelService levelService, IPlayerStatusSystemReader statusReader, IPlayerStatusSystemWriter statusWriter)
        {
            _gameState = gameState;
            _gameModeStrategy = gameModeStrategy;
            _levelService = levelService;
            _statusReader = statusReader;
            _statusWriter = statusWriter;
        }

        public override void OnNetworkSpawn()
        {
            if (IsClient)
            {
                _gameState.CurrentPhase.OnValueChanged += HandlePhaseChanged_Client;
            }
            if (IsServer)
            {
                _grid = FindObjectOfType<Grid>();
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

            _playerInstances.Remove(clientId);
            for (int i = _gameState.PlayerDatas.Count - 1; i >= 0; i--)
            {
                if (_gameState.PlayerDatas[i].ClientId == clientId)
                {
                    _gameState.PlayerDatas.RemoveAt(i);
                    break;
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

                    data.Oxygen -= oxygenDecreaseRate * Time.deltaTime;

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

        private IEnumerator FinishedPhase()
        {
            _gameState.CurrentPhase.Value = GamePhase.Finished;
            _rematchRequesters.Clear();

            // Wait for all connected players to request a rematch
            while (_rematchRequesters.Count < _playerInstances.Count)
            {
                // TODO: Add a timeout here
                yield return null;
            }

            // --- Prepare for the next round ---

            // 1. Reset scores, oxygen, etc.
            ResetPlayersForRematch();

            // 2. Regenerate map
            _levelService.RegenerateMap();

            // 3. Reposition players
            var clientIds = _playerInstances.Keys.ToList();
            var spawnPoints = _levelService.GetSpawnPoints(clientIds.Count, GetCurrentSpawnStrategy());

            for (int i = 0; i < clientIds.Count; i++)
            {
                var player = _playerInstances[clientIds[i]];
                var gridPos = spawnPoints[i];
                _levelService.ClearArea(gridPos, 1);
                var spawnPos = _grid.GetCellCenterWorld(gridPos);
                player.RespawnAt(spawnPos);
            }

            yield return null; // Wait a frame for changes to propagate before starting the next round
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

        private IEnumerator InitialSpawnPhase()
        {
            var clientIds = NetworkManager.Singleton.ConnectedClientsIds.ToList();
            int playerCount = clientIds.Count;
            var spawnPoints = _levelService.GetSpawnPoints(playerCount, GetCurrentSpawnStrategy());

            if (spawnPoints.Count < playerCount)
            {
                Debug.LogError("Not enough spawn points for players!");
                yield break;
            }

            for (int i = 0; i < playerCount; i++)
            {
                ulong clientId = clientIds[i];
                Vector3Int gridPos = spawnPoints[i];
                _levelService.ClearArea(gridPos, 1);
                Vector3 spawnPos = _grid.GetCellCenterWorld(gridPos);

                GameObject playerInstance = Instantiate(_gameConfig.PlayerPrefab, spawnPos, Quaternion.identity);
                var playerNetworkObject = playerInstance.GetComponent<NetworkObject>();
                playerNetworkObject.SpawnAsPlayerObject(clientId, true);

                // Register the instance
                var playerFacade = playerInstance.GetComponent<TypingSurvivor.Features.Game.Player.PlayerFacade>();
                _playerInstances[clientId] = playerFacade;
            }

            yield return null;
        }

        private ScriptableObject GetCurrentSpawnStrategy()
        {
            return _gameModeStrategy.PlayerCount > 1
                ? _gameConfig.VersusSpawnStrategy
                : _gameConfig.SinglePlayerSpawnStrategy;
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