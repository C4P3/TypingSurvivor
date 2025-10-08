using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using TypingSurvivor.Features.Game.Level;
using TypingSurvivor.Features.Core.PlayerStatus;
using TypingSurvivor.Features.Game.Player;
using TypingSurvivor.Features.Core.Audio;
using TypingSurvivor.Features.Game.Gameplay.Data;
using System.Collections;
using TypingSurvivor.Features.Game.Settings;
using TypingSurvivor.Features.Game.Level.Data;
using System.Threading.Tasks;
using System;

namespace TypingSurvivor.Features.Game.Gameplay
{
    public class GameManager : NetworkBehaviour, IGameStateWriter
    {
        public static GameManager Instance { get; private set; }

        private GameState _gameState;
        private IGameModeStrategy _gameModeStrategy;
        private ILevelService _levelService;
        private IPlayerStatusSystemReader _statusReader;
        private IPlayerStatusSystemWriter _statusWriter;
        private Grid _grid;
        private readonly Dictionary<ulong, PlayerFacade> _playerInstances = new();
        private readonly Dictionary<ulong, string> _clientIdToPlayerIdMap = new();

        public string GetPlayerId(ulong clientId)
        {
            _clientIdToPlayerIdMap.TryGetValue(clientId, out var playerId);
            return playerId;
        }

        private readonly HashSet<ulong> _rematchRequesters = new();
        private Coroutine _serverGameLoop;
        private GameConfig _gameConfig;
        private float oxygenDecreaseRate = 5.0f;
        private const float LowOxygenThreshold = 0.3f; // 30%
        private readonly HashSet<ulong> _playersInLowOxygen = new();
        public event System.Action<ulong, bool> OnLowOxygenStateChanged_Client;
        public event System.Func<GameResult, System.Threading.Tasks.Task<(int, int)>> OnGameFinished;
        public event Action<GameResultDto> OnResultReceived_Client;
        private Coroutine _shutdownCoroutine;

        // DTO to send all relevant result info to clients
        public struct GameResultDto : INetworkSerializable
        {
            public bool IsDraw;
            public ulong WinnerClientId;
            public float FinalGameTime;
            public PlayerData[] FinalPlayerDatas;
            public int NewWinnerRating;
            public int NewLoserRating;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref IsDraw);
                serializer.SerializeValue(ref WinnerClientId);
                serializer.SerializeValue(ref FinalGameTime);
                
                int length = 0;
                if (!serializer.IsReader)
                {
                    length = FinalPlayerDatas.Length;
                }
                serializer.SerializeValue(ref length);
                if (serializer.IsReader)
                {
                    FinalPlayerDatas = new PlayerData[length];
                }
                for (int i = 0; i < length; i++)
                {
                    FinalPlayerDatas[i].NetworkSerialize(serializer);
                }

                serializer.SerializeValue(ref NewWinnerRating);
                serializer.SerializeValue(ref NewLoserRating);
            }
        }


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

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
            // PlayerDatasリストを初期化。デフォルト名を設定。
            _gameState.PlayerDatas.Add(new PlayerData { ClientId = clientId, PlayerName = $"Player {clientId}", Oxygen = 100f, IsGameOver = false });
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (!IsServer) return;

            // Clean up low oxygen tracking
            _playersInLowOxygen.Remove(clientId);
            _clientIdToPlayerIdMap.Remove(clientId);

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

            // --- If the last player disconnects, start shutdown sequence ---
            if (_playerInstances.Count <= 0 && _shutdownCoroutine == null)
            {
                Debug.Log("[GameManager] Last player disconnected. Server will shut down.");
                _shutdownCoroutine = StartCoroutine(ShutdownServerCoroutine());
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

        [ClientRpc]
        private void SendResultsToClientsClientRpc(GameResultDto resultDto)
        {
            OnResultReceived_Client?.Invoke(resultDto);
        }

        private IEnumerator ShutdownServerCoroutine()
        {
            // Wait a short period to ensure all final messages are sent.
            yield return new WaitForSeconds(15);
            Debug.Log("[GameManager] Shutting down server.");
            Application.Quit();
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
            // The countdown will be 3 seconds. Play a sound each second.
            for (int i = 3; i > 0; i--)
            {
                SfxManager.Instance.PlaySfx(SoundId.Countdown);
                yield return new WaitForSeconds(1);
            }
            // カウントダウン終了時の音を再生
            SfxManager.Instance.PlaySfx(SoundId.CountdownEnd);
        }

        private IEnumerator PlayingPhase()
        {
            _gameState.GameTimer.Value = 0f; // Reset timer for the round
            _gameState.CurrentPhase.Value = GamePhase.Playing;
            PlayBgmClientRpc(SoundId.GameMusic);
            _playersInLowOxygen.Clear(); // Reset for the new round

            while (!_gameModeStrategy.IsGameOver(_gameState))
            {
                _gameState.GameTimer.Value += Time.deltaTime;

                // Decrease oxygen and check for low oxygen state changes
                for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
                {
                    var data = _gameState.PlayerDatas[i];
                    if (data.IsGameOver) continue;

                    // --- Oxygen Decrease Logic ---
                    float damageReduction = _statusReader.GetStatValue(data.ClientId, PlayerStat.DamageReduction);
                    damageReduction = Mathf.Clamp01(damageReduction);
                    float actualDecrease = oxygenDecreaseRate * (1.0f - damageReduction);
                    data.Oxygen -= actualDecrease * Time.deltaTime;

                    if (data.Oxygen <= 0)
                    {
                        data.Oxygen = 0;
                        data.IsGameOver = true;
                    }
                    _gameState.PlayerDatas[i] = data;
                    // --- End Oxygen Decrease ---

                    // --- Low Oxygen State Change Check ---
                    float maxOxygen = _statusReader.GetStatValue(data.ClientId, PlayerStat.MaxOxygen);
                    bool isCurrentlyLow = (data.Oxygen / maxOxygen) < LowOxygenThreshold;
                    bool wasPreviouslyLow = _playersInLowOxygen.Contains(data.ClientId);

                    if (isCurrentlyLow && !wasPreviouslyLow)
                    {
                        _playersInLowOxygen.Add(data.ClientId);
                        NotifyLowOxygenStateClientRpc(data.ClientId, true);
                    }
                    else if (!isCurrentlyLow && wasPreviouslyLow)
                    {
                        _playersInLowOxygen.Remove(data.ClientId);
                        NotifyLowOxygenStateClientRpc(data.ClientId, false);
                    }
                }
                yield return null;
            }
        }

        [ClientRpc]
        public void NotifyLowOxygenStateClientRpc(ulong clientId, bool isLowOxygen)
        {
            // Invoke the client-side event. GameUIManager will subscribe to this.
            OnLowOxygenStateChanged_Client?.Invoke(clientId, isLowOxygen);
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

            if (_gameModeStrategy is MultiPlayerStrategy || _gameModeStrategy is RankedMatchStrategy)
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
            Task finishedTask = FinishedPhaseAsync();
            while (!finishedTask.IsCompleted)
            {
                yield return null;
            }

            if (finishedTask.IsFaulted)
            {
                Debug.LogError(finishedTask.Exception);
            }
        }

        private async Task FinishedPhaseAsync()
        {
            _gameState.CurrentPhase.Value = GamePhase.Finished;
            GameResult result = _gameModeStrategy.CalculateResult(_gameState);
            PlayJingleThenMusicClientRpc(result.WinnerClientId);

            // --- レート計算　---
            int newWinnerRating = 0;
            int newLoserRating = 0;
            if (OnGameFinished != null)
            {
                var ratings = await OnGameFinished.Invoke(result);
                newWinnerRating = ratings.Item1;
                newLoserRating = ratings.Item2;
            }
            
            // 全クライエントに結果通知をブロードキャスト
            var resultDto = new GameResultDto
            {
                IsDraw = result.IsDraw,
                WinnerClientId = result.WinnerClientId,
                FinalGameTime = _gameState.GameTimer.Value,
                FinalPlayerDatas = result.FinalPlayerDatas.ToArray(),
                NewWinnerRating = newWinnerRating,
                NewLoserRating = newLoserRating
            };
            SendResultsToClientsClientRpc(resultDto);

            _rematchRequesters.Clear();

            // --- ゲームモードに応じた再戦待機ロジック ---
            bool isSinglePlayer = _gameModeStrategy is SinglePlayerStrategy;
            if (isSinglePlayer)
            {
                // シングルプレイ：　無期限待機
                _gameState.RematchTimerRemaining.Value = -1f; // -1を「無期限」のフラグとして使う
                while (_rematchRequesters.Count < _playerInstances.Count)
                {
                    // プレイヤーが切断したらループを抜ける
                    if (_playerInstances.Count < 0) break;
                    await Task.Yield();
                }
            }
            else
            {
                // マルチプレイ：　タイムアウト付き待機
                float rematchEndTime = Time.time + _gameConfig.RuleSettings.RematchTimeoutSeconds;
                while (Time.time < rematchEndTime && _rematchRequesters.Count < _playerInstances.Count)
                {
                    // 残り時間をNetworkVariable経由でクライアントに同期
                    _gameState.RematchTimerRemaining.Value = rematchEndTime - Time.time;
                    await Task.Yield();
                }
                _gameState.RematchTimerRemaining.Value = 0f; // 待機終了
            }

            // --- 再戦またはシャットダウンの判定 ---
            // 必要な人数が揃っているか、かつ全員が再戦をリクエストしたか
            if (_playerInstances.Count >= _gameModeStrategy.PlayerCount && _rematchRequesters.Count >= _gameModeStrategy.PlayerCount)
            {
                // 再戦処理へ
                Debug.Log("All players requested a rematch. Starting next round.");
                StopBgmClientRpc(0f);
                ResetPlayersForRematch();

                // マップ再生成と再配置
                // 再戦準備（マップ再生成とプレイヤー再配置）のロジック
                // Regenerate world using the same request logic as initial spawn
                var request = new MapGenerationRequest();
                var clientIds = _playerInstances.Keys.ToList();

                if (_gameModeStrategy is MultiPlayerStrategy || _gameModeStrategy is RankedMatchStrategy)
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
                await System.Threading.Tasks.Task.Yield(); // Give LevelManager a frame to process

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
                // The main game loop will now proceed to the next phase (Countdown)

            }
            else
            {
                // 再戦不成立、サーバーシャットダウンへ
                Debug.Log("Not enough players for a rematch, or timeout reached. Server will shut down.");
                if (_shutdownCoroutine == null)
                {
                    _shutdownCoroutine = StartCoroutine(ShutdownServerCoroutine());
                }
                // シャットダウンまで待機
                while (true)
                {
                    await Task.Delay(1000);
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

                // Reset runtime stats, but keep the player name
                data.IsGameOver = false;
                data.Oxygen = maxOxygen;
                data.BlocksDestroyed = 0;
                data.TypingMisses = 0;
                data.TotalTimeTyping = 0f;
                data.TotalCharsTyped = 0;
                data.TotalKeyPresses = 0;
                
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

        public void UpdatePlayerName(ulong clientId, string playerName)
        {
            if (!IsServer) return;
            for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
            {
                if (_gameState.PlayerDatas[i].ClientId == clientId)
                {
                    var data = _gameState.PlayerDatas[i];
                    data.PlayerName = playerName;
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

        public void AddBlocksDestroyed(ulong clientId, int amount)
        {
            if (!IsServer) return;
            for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
            {
                if (_gameState.PlayerDatas[i].ClientId == clientId)
                {
                    var data = _gameState.PlayerDatas[i];
                    data.BlocksDestroyed += amount;
                    _gameState.PlayerDatas[i] = data;
                    return;
                }
            }
        }

        public void AddTypingMisses(ulong clientId, int amount)
        {
            if (!IsServer) return;
            for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
            {
                if (_gameState.PlayerDatas[i].ClientId == clientId)
                {
                    var data = _gameState.PlayerDatas[i];
                    data.TypingMisses += amount;
                    _gameState.PlayerDatas[i] = data;
                    return;
                }
            }
        }

        public void AddTypingTime(ulong clientId, float time)
        {
            if (!IsServer) return;
            for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
            {
                if (_gameState.PlayerDatas[i].ClientId == clientId)
                {
                    var data = _gameState.PlayerDatas[i];
                    data.TotalTimeTyping += time;
                    _gameState.PlayerDatas[i] = data;
                    return;
                }
            }
        }

        public void AddCharsTyped(ulong clientId, int charCount)
        {
            if (!IsServer) return;
            for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
            {
                if (_gameState.PlayerDatas[i].ClientId == clientId)
                {
                    var data = _gameState.PlayerDatas[i];
                    data.TotalCharsTyped += charCount;
                    _gameState.PlayerDatas[i] = data;
                    return;
                }
            }
        }

        public void AddKeyPresses(ulong clientId, int pressCount)
        {
            if (!IsServer) return;
            for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
            {
                if (_gameState.PlayerDatas[i].ClientId == clientId)
                {
                    var data = _gameState.PlayerDatas[i];
                    data.TotalKeyPresses += pressCount;
                    _gameState.PlayerDatas[i] = data;
                    return;
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RegisterPlayerIdServerRpc(string playerId, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            if (!_clientIdToPlayerIdMap.ContainsKey(clientId))
            {
                _clientIdToPlayerIdMap[clientId] = playerId;
                Debug.Log($"[GameManager] Registered PlayerId {playerId} for ClientId {clientId}");
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

        // --- Music Control RPCs ---
        [ClientRpc]
        private void PlayBgmClientRpc(SoundId bgmId)
        {
            MusicManager.Instance.Play(bgmId, 0f);
        }

        // BGM停止用のRPC
        [ClientRpc]
        private void StopBgmClientRpc(float fadeDuration)
        {
            MusicManager.Instance.Stop(fadeDuration);
        }

        [ClientRpc]
        private void PlayJingleThenMusicClientRpc(ulong winnerId)
        {
            bool localPlayerWon = winnerId == NetworkManager.Singleton.LocalClientId;
            var jingleId = localPlayerWon ? SoundId.WinJingle : SoundId.LoseJingle;
            
            MusicManager.Instance.PlayJingleThen(jingleId, SoundId.ResultsMusic, 1.0f, 0.5f);
        }
    }
}