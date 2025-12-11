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
        private GameState _gameState;
        private IGameModeStrategy _gameModeStrategy;
        private ILevelService _levelService;
        private IPlayerStatusSystemReader _statusReader;
        private IPlayerStatusSystemWriter _statusWriter;
        private Grid _grid;
        private readonly Dictionary<ulong, PlayerFacade> _playerInstances = new();
        private readonly Dictionary<ulong, string> _clientIdToPlayerIdMap = new();

        // --- Oxygen Change Management ---
        private readonly Dictionary<ulong, float> _oxygenDeltaThisFrame = new();

        public string GetPlayerId(ulong clientId)
        {
            _clientIdToPlayerIdMap.TryGetValue(clientId, out var playerId);
            return playerId;
        }

        private readonly HashSet<ulong> _rematchRequesters = new();
        private Coroutine _serverGameLoop;
        private GameConfig _gameConfig;

        // --- Oxygen Decrease Settings ---
        // Phase 1 (Linear)
        [Header("Debug Settings")]
        [SerializeField] private float _oxygenDecreaseStartRate = 2.5f;
        [SerializeField] private float _oxygenDecreaseMidRate = 5.0f;
        private float _debugOxygenMultiplier = 1.0f; // デバッグ用倍率
        private const float TIME_TO_SWITCH_TO_LOG = 300.0f; // 10 minutes
        private float _linearRateOfChange;

        // Phase 2 (Logarithmic)
        private const float LOG_TIME_SCALE = 60.0f; // Scales the time input for the log function
        private float _logCoefficient;
        // --- End Oxygen Decrease Settings ---

        private const float LowOxygenThreshold = 0.3f; // 30%
        private readonly HashSet<ulong> _playersInLowOxygen = new();
        public event System.Action<ulong, bool> OnLowOxygenStateChanged_Client;
        public event System.Func<GameResult, System.Threading.Tasks.Task<(int, int, int, int)>> OnGameFinished;
        public event Action<GameResultDto> OnResultReceived_Client;
        public event Action<RatingsDto> OnRatingsCalculated_Client; // For async rating updates
        public event Action OnOpponentDisconnectedInGame_Client;
        public event Action OnOpponentDisconnectedResult_Client;
        public event Action OnReturnToMainMenu_Client;
        public event Action<int, int> OnRematchStatusChanged_Client;
        private Coroutine _shutdownCoroutine;

        // DTO for async rating updates
        public struct RatingsDto : INetworkSerializable
        {
            public int OldWinnerRating;
            public int NewWinnerRating;
            public int OldLoserRating;
            public int NewLoserRating;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref OldWinnerRating);
                serializer.SerializeValue(ref NewWinnerRating);
                serializer.SerializeValue(ref OldLoserRating);
                serializer.SerializeValue(ref NewLoserRating);
            }
        }

        // DTO to send all relevant result info to clients

        // DTO to send all relevant result info to clients
        public struct GameResultDto : INetworkSerializable
        {
            public bool IsDraw;
            public ulong WinnerClientId;
            public float FinalGameTime;
            public PlayerData[] FinalPlayerDatas;
            public int OldWinnerRating;
            public int NewWinnerRating;
            public int OldLoserRating;
            public int NewLoserRating;
            public bool OpponentDisconnected; // Added to indicate disconnection

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

                serializer.SerializeValue(ref OldWinnerRating);
                serializer.SerializeValue(ref NewWinnerRating);
                serializer.SerializeValue(ref OldLoserRating);
                serializer.SerializeValue(ref NewLoserRating);
                serializer.SerializeValue(ref OpponentDisconnected);
            }
        }

        public void Initialize(
            GameState gameState, IGameModeStrategy gameModeStrategy, ILevelService levelService, GameConfig gameConfig, Grid grid, 
#nullable enable
            IPlayerStatusSystemReader? statusReader = null, IPlayerStatusSystemWriter? statusWriter = null
#nullable disable
        )
        {
            _gameState = gameState;
            _gameModeStrategy = gameModeStrategy;
            _levelService = levelService;
            _gameConfig = gameConfig;
            _grid = grid;
            if (statusReader != null && statusWriter != null)
            {
                _statusReader = statusReader;
                _statusWriter = statusWriter;
            }
        }

        public override void OnNetworkSpawn()
        {
            // Logic moved to StartGameLoop() to be called after all dependencies are injected.
        }

        public void StartGameLoop()
        {
            if (IsClient)
            {
                _gameState.CurrentPhase.OnValueChanged += HandlePhaseChanged_Client;
            }
            if (IsServer)
            {
                // --- Calculate oxygen decrease rates ---
                _linearRateOfChange = (_oxygenDecreaseMidRate - _oxygenDecreaseStartRate) / TIME_TO_SWITCH_TO_LOG;
                _logCoefficient = _linearRateOfChange * LOG_TIME_SCALE;
                // ---

                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;

                // --- PlayerStatusSystem Refactor ---
                _statusReader.OnStatChanged += HandlePlayerStatChanged;
                // --- PlayerStatusSystem Refactor ---
                
                // Initialize players already connected
                foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    HandleClientConnected(clientId);
                }

                _serverGameLoop = StartCoroutine(ServerGameLoop());
            }
        }

        // 外部から酸素減少量を変更するためのメソッド
        public void SetOxygenDepletionMultiplier(float multiplier)
        {
            if (!IsServer) return;
            _debugOxygenMultiplier = multiplier;
            Debug.Log($"[GameManager] Oxygen depletion multiplier set to: {_debugOxygenMultiplier}");
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

                // --- PlayerStatusSystem Refactor ---
                if (_statusReader != null) _statusReader.OnStatChanged -= HandlePlayerStatChanged;
                // --- PlayerStatusSystem Refactor ---
            }
        }

        private void LateUpdate()
        {
            if (!IsServer) return;
            if (_oxygenDeltaThisFrame.Count == 0) return;

            // Apply all accumulated oxygen changes for this frame
            for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
            {
                var data = _gameState.PlayerDatas[i];
                if (_oxygenDeltaThisFrame.TryGetValue(data.ClientId, out float delta))
                {
                    // PlayerDataにMaxOxygenが追加されたので、そちらを参照する
                    data.Oxygen = Mathf.Clamp(data.Oxygen + delta, 0, data.MaxOxygen);
                    _gameState.PlayerDatas[i] = data;
                }
            }

            _oxygenDeltaThisFrame.Clear();
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (!IsServer) return;
            // PlayerDatasリストを初期化。デフォルト名を設定。
            var initialData = new PlayerData
            {
                ClientId = clientId,
                PlayerName = $"Player {clientId}",
                IsGameOver = false,
                // --- PlayerStatusSystem Refactor ---
                // 初期ステータスをPlayerStatusSystemから取得して設定
                MoveSpeed = _statusReader.GetStatValue(clientId, PlayerStat.MoveSpeed),
                MaxOxygen = _statusReader.GetStatValue(clientId, PlayerStat.MaxOxygen),
                RadarRange = _statusReader.GetStatValue(clientId, PlayerStat.RadarRange),
                DamageReduction = _statusReader.GetStatValue(clientId, PlayerStat.DamageReduction)
            };
            initialData.Oxygen = initialData.MaxOxygen; // 初期酸素は最大値
            _gameState.PlayerDatas.Add(initialData);
            // --- PlayerStatusSystem Refactor ---
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (!IsServer) return;

            // Find the player in our list and mark them as disconnected.
            int disconnectedPlayerIndex = -1;
            for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
            {
                if (_gameState.PlayerDatas[i].ClientId == clientId)
                {
                    var data = _gameState.PlayerDatas[i];
                    data.IsDisconnected = true;
                    data.IsGameOver = true; // Also mark as game over to trigger end-game logic
                    _gameState.PlayerDatas[i] = data;
                    disconnectedPlayerIndex = i;
                    break;
                }
            }

            // If the player was not in the list (e.g., disconnected before being added), there's nothing to do.
            if (disconnectedPlayerIndex == -1) return;

            // Always notify remaining players about the disconnection.
            ShowInGameOpponentDisconnectedClientRpc();

            // If the game is already on the result screen, send a specific notification to update that UI.
            if (_gameState.CurrentPhase.Value == GamePhase.Finished)
            {
                NotifyResultScreenOpponentDisconnectedClientRpc();
            }
            
            // The main ServerGameLoop will now naturally detect the game over state via IsGameOver()
            // and transition to the Finished phase, whether the game was in Countdown, Playing, or WaitingForPlayers.
        }

        [ClientRpc]
        private void SendResultsToClientsClientRpc(GameResultDto resultDto)
        {
            OnResultReceived_Client?.Invoke(resultDto);
        }

        [ClientRpc]
        private void UpdateRatingsOnResultScreenClientRpc(RatingsDto ratingsDto)
        {
            OnRatingsCalculated_Client?.Invoke(ratingsDto);
        }

        [ClientRpc]
        private void ShowInGameOpponentDisconnectedClientRpc()
        {
            OnOpponentDisconnectedInGame_Client?.Invoke();
        }

        [ClientRpc]
        private void NotifyResultScreenOpponentDisconnectedClientRpc()
        {
            OnOpponentDisconnectedResult_Client?.Invoke();
        }

        [ClientRpc]
        private void ReturnToMainMenuClientRpc()
        {
            OnReturnToMainMenu_Client?.Invoke();
        }

        [ClientRpc]
        private void UpdateRematchStatusClientRpc(int requesterCount, int totalPlayers)
        {
            OnRematchStatusChanged_Client?.Invoke(requesterCount, totalPlayers);
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
                PlaySfxClientRpc(SoundId.Countdown);
                yield return new WaitForSeconds(1);
            }
            // カウントダウン終了時の音を再生
            PlaySfxClientRpc(SoundId.CountdownEnd);
            yield return new WaitForSeconds(0.1f);
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

                // --- Calculate current oxygen decrease rate based on game time ---
                float gameTime = _gameState.GameTimer.Value;
                float currentRate;

                if (gameTime <= TIME_TO_SWITCH_TO_LOG)
                {
                    // Phase 1: Linear increase
                    currentRate = _oxygenDecreaseStartRate + (gameTime * _linearRateOfChange);
                }
                else
                {
                    // Phase 2: Logarithmic increase
                    float timeSinceSwitch = gameTime - TIME_TO_SWITCH_TO_LOG;
                    currentRate = _oxygenDecreaseMidRate + _logCoefficient * Mathf.Log(timeSinceSwitch / LOG_TIME_SCALE + 1);
                }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                currentRate *= _debugOxygenMultiplier;
#endif
                // ---

                // Decrease oxygen and check for low oxygen state changes
                for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
                {
                    var data = _gameState.PlayerDatas[i];
                    if (data.IsGameOver) continue;

                    // --- Oxygen Decrease Logic ---
                    // PlayerDataにDamageReductionが追加されたので、そちらを参照する
                    float damageReduction = data.DamageReduction;
                    damageReduction = Mathf.Clamp01(damageReduction);
                    float actualDecrease = currentRate * (1.0f - damageReduction);
                    
                    // Record the decrease in the delta dictionary
                    if (!_oxygenDeltaThisFrame.ContainsKey(data.ClientId)) _oxygenDeltaThisFrame[data.ClientId] = 0;
                    _oxygenDeltaThisFrame[data.ClientId] -= actualDecrease * Time.deltaTime;

                    // Check if oxygen will be depleted this frame to set game over state
                    if (data.Oxygen + _oxygenDeltaThisFrame[data.ClientId] <= 0)
                    {
                        var mutableData = _gameState.PlayerDatas[i];
                        mutableData.IsGameOver = true;
                        _gameState.PlayerDatas[i] = mutableData;
                    }
                    // --- End Oxygen Decrease ---

                    // --- Low Oxygen State Change Check ---
                    // PlayerDataにMaxOxygenが追加されたので、そちらを参照する
                    bool isCurrentlyLow = (data.Oxygen / data.MaxOxygen) < LowOxygenThreshold;
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

                    // Instantiate at (0,0) and then explicitly set position after spawning
                    // to avoid position reset during Netcode's spawn process.
                    GameObject playerInstance = Instantiate(_gameConfig.PlayerPrefab, Vector3.zero, Quaternion.identity);
                    
                    var playerNetworkObject = playerInstance.GetComponent<NetworkObject>();
                    playerNetworkObject.SpawnAsPlayerObject(clientId, true);

                    var playerFacade = playerInstance.GetComponent<TypingSurvivor.Features.Game.Player.PlayerFacade>();

                    // Explicitly set position and grid data on the server after spawning.
                    playerInstance.transform.position = spawnPos;
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
                // If the finish phase fails catastrophically, stop the entire game loop
                // to prevent infinitely attempting to restart the round.
                if (_serverGameLoop != null)
                {
                    StopCoroutine(_serverGameLoop);
                }
            }
        }

        private async Task FinishedPhaseAsync()
        {
            _gameState.CurrentPhase.Value = GamePhase.Finished;
            GameResult result = _gameModeStrategy.CalculateResult(_gameState);
            PlayJingleThenMusicClientRpc(result.WinnerClientId);

            // --- Handle single player score submission ---
            if (_gameModeStrategy is SinglePlayerStrategy)
            {
                var leaderboardService = Core.App.AppManager.Instance.GetService<Core.Leaderboard.ISurvivalLeaderboardService>();
                if (leaderboardService != null)
                {
                    float survivalTime = _gameState.GameTimer.Value;
                    _ = leaderboardService.SubmitScoreAsync(survivalTime);
                    Debug.Log($"[GameManager] Submitted single player survival time: {survivalTime}");
                }
            }

            // --- Send initial result to clients immediately ---
            bool opponentDisconnected = false;
            foreach (var p in _gameState.PlayerDatas)
            {
                if (p.IsDisconnected)
                {
                    opponentDisconnected = true;
                    break;
                }
            }
            var initialResultDto = new GameResultDto
            {
                IsDraw = result.IsDraw,
                WinnerClientId = result.WinnerClientId,
                FinalGameTime = _gameState.GameTimer.Value,
                FinalPlayerDatas = result.FinalPlayerDatas.ToArray(),
                OpponentDisconnected = opponentDisconnected,
                // Rating values are default (0) and will be sent later
            };
            SendResultsToClientsClientRpc(initialResultDto);

            // --- Handle ranked match rating calculation in the background ---
            if (OnGameFinished != null && _gameModeStrategy is RankedMatchStrategy)
            {
                CalculateAndSendRatings_FireAndForget(result);
            }

            // --- Rematch logic can now proceed without waiting for rating calculation ---
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

            // Clean up players who disconnected during the match before deciding on rematch.
            CleanupDisconnectedPlayers();

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
                ReturnToMainMenuClientRpc();
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

            // Reset any pending oxygen changes from the previous round
            _oxygenDeltaThisFrame.Clear();

            for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
            {
                // 1. PlayerDataの現在のコピーを取得（ClientIdの取得が主目的）
                var data = _gameState.PlayerDatas[i];
                
                // 2. まずStatusSystemの内部状態をリセットする
                _statusWriter.ClearSessionModifiers(data.ClientId);
                
                // 3. イベントに頼らず、StatusSystem(Source of Truth)からリセット後の「ベース値」を"PULL"する
                //    これにより、「値が変わらないからイベントが飛ばない」問題を回避する
                data.MoveSpeed = _statusReader.GetStatValue(data.ClientId, PlayerStat.MoveSpeed);
                data.MaxOxygen = _statusReader.GetStatValue(data.ClientId, PlayerStat.MaxOxygen);
                data.RadarRange = _statusReader.GetStatValue(data.ClientId, PlayerStat.RadarRange);
                data.DamageReduction = _statusReader.GetStatValue(data.ClientId, PlayerStat.DamageReduction);

                // 4. ランタイムのステータスをリセットする
                data.IsGameOver = false;
                data.Oxygen = data.MaxOxygen;
                data.BlocksDestroyed = 0;
                data.TypingMisses = 0;
                data.TotalTimeTyping = 0f;
                data.TotalCharsTyped = 0;
                data.TotalKeyPresses = 0;
                
                // 5. 完全にリセットされた `data` をGameStateに書き戻す
                _gameState.PlayerDatas[i] = data;
            }
        }

        // --- IGameStateWriter Implementation ---
        public void AddOxygen(ulong clientId, float amount)
        {
            if (!IsServer) return;

            if (!_oxygenDeltaThisFrame.ContainsKey(clientId))
            {
                _oxygenDeltaThisFrame[clientId] = 0;
            }
            if(amount > 0)
            {
                _oxygenDeltaThisFrame[clientId] += amount;
            }
            else
            {
                // PlayerDataにDamageReductionが追加されたので、そちらを参照する
                float damageReduction = 0f;
                for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
                {
                    if (_gameState.PlayerDatas[i].ClientId == clientId)
                    {
                        damageReduction = _gameState.PlayerDatas[i].DamageReduction;
                        break;
                    }
                }
                damageReduction = Mathf.Clamp01(damageReduction);
                float actualDecrease = amount * (1.0f - damageReduction);
                _oxygenDeltaThisFrame[clientId] += actualDecrease;
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
                _gameState.RematchRequesterCount.Value = _rematchRequesters.Count;
                UpdateRematchStatusClientRpc(_rematchRequesters.Count, _playerInstances.Count);
                Debug.Log($"Player {clientId} requested a rematch. {_rematchRequesters.Count}/{_playerInstances.Count}");
            }
        }

        // --- Music Control RPCs ---
        [ClientRpc]
        private void PlaySfxClientRpc(SoundId sfxId)
        {
            SfxManager.Instance.PlaySfx(sfxId);
        }


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

        private void CleanupDisconnectedPlayers()
        {
            for (int i = _gameState.PlayerDatas.Count - 1; i >= 0; i--)
            {
                if (_gameState.PlayerDatas[i].IsDisconnected)
                {
                    ulong clientId = _gameState.PlayerDatas[i].ClientId;

                    // Clean up game state and tracking collections
                    _playersInLowOxygen.Remove(clientId);
                    _clientIdToPlayerIdMap.Remove(clientId);

                    if (_playerInstances.TryGetValue(clientId, out var playerFacade))
                    {
                        // Remove from the synced list of spawned players
                        for (int j = _gameState.SpawnedPlayers.Count - 1; j >= 0; j--)
                        {
                            if (_gameState.SpawnedPlayers[j].TryGet(out var networkObject) && networkObject == playerFacade.NetworkObject)
                            {
                                _gameState.SpawnedPlayers.RemoveAt(j);
                                break;
                            }
                        }
                        _playerInstances.Remove(clientId);
                    }

                    _gameState.PlayerDatas.RemoveAt(i);
                }
            }
        }

        private async void CalculateAndSendRatings_FireAndForget(GameResult result)
        {
            try
            {
                var ratings = await OnGameFinished.Invoke(result);
                var ratingsDto = new RatingsDto
                {
                    OldWinnerRating = ratings.Item1,
                    NewWinnerRating = ratings.Item2,
                    OldLoserRating = ratings.Item3,
                    NewLoserRating = ratings.Item4,
                };
                UpdateRatingsOnResultScreenClientRpc(ratingsDto);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameManager] Failed to calculate and send ratings: {e.Message}");
            }
        }

        // --- PlayerStatusSystem Refactor ---
        private void HandlePlayerStatChanged(ulong clientId, PlayerStat stat, float newValue)
        {
            for (int i = 0; i < _gameState.PlayerDatas.Count; i++)
            {
                if (_gameState.PlayerDatas[i].ClientId == clientId)
                {
                    var data = _gameState.PlayerDatas[i];
                    switch (stat)
                    {
                        case PlayerStat.MoveSpeed:
                            data.MoveSpeed = newValue;
                            break;
                        case PlayerStat.MaxOxygen:
                            data.MaxOxygen = newValue;
                            break;
                        case PlayerStat.RadarRange:
                            data.RadarRange = newValue;
                            break;
                        case PlayerStat.DamageReduction:
                            data.DamageReduction = newValue;
                            break;
                    }
                    _gameState.PlayerDatas[i] = data;
                    return;
                }
            }
        }
        // --- PlayerStatusSystem Refactor ---
    }
}