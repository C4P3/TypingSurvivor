using System.Collections.Generic;
using TypingSurvivor.Features.Core.Audio;
using TypingSurvivor.Features.Core.PlayerStatus;
using TypingSurvivor.Features.Core.VFX;
using TypingSurvivor.Features.Game.Camera;
using TypingSurvivor.Features.Game.Gameplay;
using TypingSurvivor.Features.Game.Gameplay.Data;
using System.Linq;
using TypingSurvivor.Features.Game.Typing;
using TypingSurvivor.Features.UI.Screens;
using TypingSurvivor.Features.UI.Screens.InGameHUD;
using TypingSurvivor.Features.UI.Screens.Global; // Changed
using Unity.Netcode;
using UnityEngine;
using TypingSurvivor.Features.Core.CloudSave;
using TypingSurvivor.Features.Core.Leaderboard;
using TypingSurvivor.Features.UI.Common;

namespace TypingSurvivor.Features.UI
{
    [RequireComponent(typeof(UIManager))]
    public class GameUIManager : MonoBehaviour
    {
        [Header("UI System")]
        [SerializeField] private UIManager _uiManager;

        [Header("Screen & Prefab References")]
        [SerializeField] private InGameHUDManager _inGameHUDPrefab; // Prefab to instantiate
        [SerializeField] private ResultScreen _resultScreen;
        [SerializeField] private CountdownScreen _countdownScreen;
        [SerializeField] private InGameGlobalView _inGameGlobalView; // Changed
        [SerializeField] private NotificationPanel _notificationPanelPrefab;

        [Header("Low Oxygen Effect")]
        [SerializeField] private float _lowOxygenPitch = 1.2f;
        
        private IGameStateReader _gameStateReader;
        private IPlayerStatusSystemReader _playerStatusReader;
        private GameManager _gameManager;
        private ITypingService _typingService;
        private CameraManager _cameraManager;
        private ICloudSaveService _cloudSaveService;
        private ISurvivalLeaderboardService _survivalLeaderboardService; // For future use

        private readonly Dictionary<ulong, InGameHUDManager> _playerHuds = new();
        private NetworkList<NetworkObjectReference>.OnListChangedDelegate _onPlayerListChangedHandler;
        private readonly Dictionary<ulong, Coroutine> _blinkingCoroutines = new();
        private readonly Dictionary<ulong, LowHealthEffect> _activeLowHealthEffects = new();

        protected virtual void Awake()
        {
            if (_uiManager == null) _uiManager = GetComponent<UIManager>();
            _onPlayerListChangedHandler = OnPlayerListChanged;
        }

        public void Initialize(IGameStateReader gameStateReader, IPlayerStatusSystemReader playerStatusReader, GameManager gameManager, ITypingService typingService, CameraManager cameraManager, ICloudSaveService cloudSaveService, ISurvivalLeaderboardService survivalLeaderboardService)
        {
            _gameStateReader = gameStateReader;
            _playerStatusReader = playerStatusReader;
            _gameManager = gameManager;
            _typingService = typingService;
            _cameraManager = cameraManager;
            _cloudSaveService = cloudSaveService;
            _survivalLeaderboardService = survivalLeaderboardService; // For future use

            if (_inGameGlobalView != null)
            {
                _inGameGlobalView.Initialize(gameStateReader);
            }

            SubscribeToEvents();
            
            // Handle cameras that might have been assigned before we subscribed.
            var existingCameras = _cameraManager.GetAssignedCameras();
            foreach (var pair in existingCameras)
            {
                HandleCameraAssigned(pair.Key, pair.Value);
            }

            HandlePhaseChanged(default, _gameStateReader.CurrentPhaseNV.Value);
            UpdatePlayerHUDs(); // Initial HUD cleanup for any players that might have left
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            foreach (var hud in _playerHuds.Values) if (hud != null) Destroy(hud.gameObject);
            _playerHuds.Clear();
        }

        private void SubscribeToEvents()
        {
            _gameStateReader.CurrentPhaseNV.OnValueChanged += HandlePhaseChanged;
            _gameStateReader.RematchTimerRemainingNV.OnValueChanged += HandleRematchTimerChanged; // タイマーの購読
            _gameStateReader.PlayerDatas.OnListChanged += HandlePlayerDataChanged; // プレイヤーリストの購読
            _gameStateReader.SpawnedPlayers.OnListChanged += _onPlayerListChangedHandler;
            _resultScreen.OnRematchClicked += HandleRematchClicked;
            _resultScreen.OnMainMenuClicked += HandleMainMenuClicked;
            _gameManager.OnLowOxygenStateChanged_Client += HandleLowOxygenStateChange;
            _gameManager.OnResultReceived_Client += HandleResultReceived;
            _gameManager.OnOpponentDisconnectedInGame_Client += HandleOpponentDisconnectedInGame;
            _gameManager.OnOpponentDisconnectedResult_Client += HandleOpponentDisconnectedResult;
            _gameManager.OnReturnToMainMenu_Client += HandleReturnToMainMenu;
            _gameManager.OnRematchStatusChanged_Client += HandleRematchStatusChanged;
            _cameraManager.OnCameraAssigned += HandleCameraAssigned;

            if (_typingService != null)
            {
                _typingService.OnTypingProgressed += HandleTypingProgressed;
                _typingService.OnTypingCancelled += HandleTypingCancelled;
                _typingService.OnTypingSuccess += HandleTypingSuccess;
                _typingService.OnTypingMiss += HandleTypingMiss;
            }

            if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_gameStateReader != null)
            {
                _gameStateReader.CurrentPhaseNV.OnValueChanged -= HandlePhaseChanged;
                _gameStateReader.RematchTimerRemainingNV.OnValueChanged -= HandleRematchTimerChanged;
                _gameStateReader.PlayerDatas.OnListChanged -= HandlePlayerDataChanged;
                _gameStateReader.SpawnedPlayers.OnListChanged -= _onPlayerListChangedHandler;
            }
            if (_resultScreen != null) 
            {
                _resultScreen.OnRematchClicked -= HandleRematchClicked;
                _resultScreen.OnMainMenuClicked -= HandleMainMenuClicked;
            }
            if (_gameManager != null)
            {
                _gameManager.OnLowOxygenStateChanged_Client -= HandleLowOxygenStateChange;
                _gameManager.OnResultReceived_Client -= HandleResultReceived;
                _gameManager.OnOpponentDisconnectedInGame_Client -= HandleOpponentDisconnectedInGame;
                _gameManager.OnOpponentDisconnectedResult_Client -= HandleOpponentDisconnectedResult;
                _gameManager.OnReturnToMainMenu_Client -= HandleReturnToMainMenu;
                _gameManager.OnRematchStatusChanged_Client -= HandleRematchStatusChanged;
            }
            if (_cameraManager != null) _cameraManager.OnCameraAssigned -= HandleCameraAssigned;
            
            if (_typingService != null)
            {
                _typingService.OnTypingProgressed -= HandleTypingProgressed;
                _typingService.OnTypingCancelled -= HandleTypingCancelled;
                _typingService.OnTypingSuccess -= HandleTypingSuccess;
                _typingService.OnTypingMiss -= HandleTypingMiss;
            }

            if (NetworkManager.Singleton != null) NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }

        private void HandleOpponentDisconnectedInGame()
        {
            if (_notificationPanelPrefab == null) return;
            var popup = Instantiate(_notificationPanelPrefab, transform);
            popup.Show("対戦相手が切断しました。", 3f);
        }

        private void HandleOpponentDisconnectedResult()
        {
            _resultScreen.CurrentView?.NotifyOpponentDisconnected();
        }

        private void HandleReturnToMainMenu()
        {
            ReturnToMainMenu();
        }

        private void HandleRematchStatusChanged(int count, int total)
        {
            _resultScreen.CurrentView?.UpdateRematchRequesterCount(count, total);
        }

        private void HandleCameraAssigned(ulong clientId, UnityEngine.Camera camera)
        {
            if (_playerHuds.ContainsKey(clientId))
            {
                // HUD already exists, just ensure camera is set.
                _playerHuds[clientId].SetRenderCamera(camera);
                return;
            }

            // HUD doesn't exist, create it now.
            var newHud = Instantiate(_inGameHUDPrefab, transform);
            newHud.gameObject.name = $"PlayerHUD_{clientId}";
            
            // Initialize and set camera immediately
            newHud.SetPlayerOwnerId(clientId);
            newHud.Initialize(_gameStateReader, _playerStatusReader);
            newHud.SetRenderCamera(camera);
            _playerHuds[clientId] = newHud;

            // Fetch rating for ranked matches
            if (Core.App.AppManager.Instance.GameMode == Core.App.GameModeType.RankedMatch)
            {
                FetchAndDisplayRating(newHud, clientId);
            }
            
            // Ensure visibility is correct for the current phase
            if (_gameStateReader != null)
            {
                if (_gameStateReader.CurrentPhaseNV.Value == GamePhase.Playing) newHud.Show();
                else newHud.Hide();
            }
        }

        private void OnPlayerListChanged(Unity.Netcode.NetworkListEvent<Unity.Netcode.NetworkObjectReference> changeEvent)
        {
            // This event is now only for cleaning up HUDs of disconnected players.
            UpdatePlayerHUDs();
        }

        private void UpdatePlayerHUDs()
        {
            if (_gameStateReader == null) return;

            var activePlayerIds = new HashSet<ulong>();
            foreach (var playerRef in _gameStateReader.SpawnedPlayers)
            {
                if (playerRef.TryGet(out var netObj))
                {
                    activePlayerIds.Add(netObj.OwnerClientId);
                }
            }

            var hudsToRemove = _playerHuds.Keys.Where(id => !activePlayerIds.Contains(id)).ToList();
            foreach (var id in hudsToRemove)
            {
                if (_playerHuds.TryGetValue(id, out var hud))
                {
                    Destroy(hud.gameObject);
                }
                _playerHuds.Remove(id);
            }
        }

        private async void FetchAndDisplayRating(InGameHUDManager hud, ulong clientId)
        {
            if (_cloudSaveService == null || _gameManager == null) return;

            string ugsPlayerId = _gameManager.GetPlayerId(clientId);
            if (string.IsNullOrEmpty(ugsPlayerId))
            {
                // Maybe the ID hasn't been registered yet. This can be improved with a retry or event.
                return;
            }

            int rating = await _cloudSaveService.GetRatingAsync(ugsPlayerId);
            bool isRanked = Core.App.AppManager.Instance.GameMode == Core.App.GameModeType.RankedMatch;
            
            hud.UpdatePlayerRating(rating, isRanked);
        }

        private void HandlePhaseChanged(GamePhase previousPhase, GamePhase newPhase)
        {
            foreach (var hud in _playerHuds.Values)
            {
                if (newPhase == GamePhase.Playing) hud.Show();
                else hud.Hide();
            }

            if (_inGameGlobalView != null && newPhase == GamePhase.Playing)
            {
                 _inGameGlobalView.Show();
            }
            else if (_inGameGlobalView != null)
            {
                _inGameGlobalView.Hide();
            }

            switch (newPhase)
            {
                case GamePhase.WaitingForPlayers:
                    _uiManager.ShowScreen(null);
                    break;
                case GamePhase.Countdown:
                    _uiManager.ShowScreen(_countdownScreen);
                    _countdownScreen.StartCountdown(() => {});
                    break;
                case GamePhase.Playing:
                    _uiManager.ShowScreen(null);
                    break;
                case GamePhase.Finished:
                    ResetLowOxygenEffects();
                    _uiManager.ShowScreen(_resultScreen);
                    // The showing of the result screen is now handled by HandleResultReceived
                    break;
            }
        }
        private void HandleRematchTimerChanged(float previousValue, float newValue)
        {
            // ResultScreenが表示されている場合のみ、タイマー情報をビューに渡す
            if (_gameStateReader.CurrentPhaseNV.Value == GamePhase.Finished && _resultScreen.CurrentView != null)
            {
                _resultScreen.CurrentView.UpdateRematchTimer(newValue);
            }
        }

        private void HandlePlayerDataChanged(NetworkListEvent<PlayerData> changeEvent)
        {
            // Finishedフェーズで、プレイヤーがリストから「削除」された場合に通知
            if (_gameStateReader.CurrentPhaseNV.Value == GamePhase.Finished &&
                changeEvent.Type == NetworkListEvent<PlayerData>.EventType.Remove)
            {
                // マルチプレイモードの場合のみ処理
                if (Core.App.AppManager.Instance.GameMode != Core.App.GameModeType.SinglePlayer)
                {
                    _resultScreen.CurrentView?.NotifyOpponentDisconnected();
                }
            }
        }

        #region Typing Handlers

        private void HandleTypingSuccess()
        {
            SfxManager.Instance.PlaySfx(SoundId.TypingSuccess);
            if (_inGameGlobalView != null) _inGameGlobalView.SetTypingActive(false);
        }

        private void HandleTypingMiss()
        {
            SfxManager.Instance.PlaySfx(SoundId.TypingMiss);
        }

        private void HandleTypingProgressed()
        {
            if (_inGameGlobalView == null) return;

            if (_typingService.IsTyping)
            {
                _inGameGlobalView.SetTypingActive(true);
                _inGameGlobalView.UpdateTypingQuestion(_typingService.GetDisplayText());
                _inGameGlobalView.UpdateTypingInput(_typingService.GetTypedRomaji(), _typingService.GetRemainingRomaji());
                SfxManager.Instance.PlaySfx(SoundId.TypingKeyPress);
            }
            else
            {
                _inGameGlobalView.SetTypingActive(false);
            }
        }

        private void HandleTypingCancelled()
        {
            if (_inGameGlobalView != null) _inGameGlobalView.SetTypingActive(false);
        }

        #endregion

        #region Event Handlers

        private void HandleResultReceived(GameManager.GameResultDto resultDto)
        {
            // For single player, process high score logic before showing the screen
            if (Core.App.AppManager.Instance.GameMode == Core.App.GameModeType.SinglePlayer)
            {
                ProcessSinglePlayerResultAsync(resultDto);
            }
            else
            {
                _resultScreen.Show(resultDto, 0, 0, 0); // No high score or rank info for multiplaye
            }
        }

        private async void ProcessSinglePlayerResultAsync(GameManager.GameResultDto resultDto)
        {
            float survivalTime = resultDto.FinalGameTime;
            var appManager = Core.App.AppManager.Instance;

            // 1. Use cached data for immediate display
            float personalBest = appManager.CachedPlayerData?.Progress.SinglePlayHighScore ?? 0;
            int playerRank = appManager.CachedRankData.playerRank;
            int totalPlayers = appManager.CachedRankData.totalPlayers;

            // 2. Show the result screen immediately with cached data
            _resultScreen.Show(resultDto, personalBest, playerRank, totalPlayers);

            // 3. In the background, submit the new score and update save data if needed
            if (survivalTime > personalBest)
            {
                // Update leaderboard
                _survivalLeaderboardService?.SubmitScoreAsync(survivalTime);

                // Update Cloud Save
                var saveData = appManager.CachedPlayerData ?? new PlayerSaveData();
                saveData.Progress.SinglePlayHighScore = survivalTime;
                await _cloudSaveService.SavePlayerDataAsync(saveData);
                
                // Update the cache in AppManager as well
                appManager.CachedPlayerData = saveData;
            }
        }

        private void HandleRematchClicked()
        {
            _gameManager.RequestRematchServerRpc();
        }

        private void HandleMainMenuClicked()
        {
            ReturnToMainMenu();
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId) return;

            if (_notificationPanelPrefab == null) return;
            var popup = Instantiate(_notificationPanelPrefab, transform);
            popup.Show("サーバーとの接続が切断されました。", null, "メインメニューへ戻る", ReturnToMainMenu);
        }

        #endregion

        #region Specific UI Logic

        private void ResetLowOxygenEffects()
        {
            foreach (var coroutine in _blinkingCoroutines.Values) StopCoroutine(coroutine);
            _blinkingCoroutines.Clear();

            foreach (var effect in _activeLowHealthEffects.Values) if (effect != null) effect.SetOpacity(0f);
            _activeLowHealthEffects.Clear();
            
            if(MusicManager.Instance != null) MusicManager.Instance.ResetPitch();
        }

        public void HandleLowOxygenStateChange(ulong clientId, bool isLowOxygen)
        {
            var playerCamera = _cameraManager.GetCameraForPlayer(clientId);
            if (playerCamera == null) return;

            var lowHealthEffect = playerCamera.GetComponent<LowHealthEffect>();
            if (lowHealthEffect == null) return;

            if (isLowOxygen)
            {
                if (!_blinkingCoroutines.ContainsKey(clientId))
                {
                    var coroutine = StartCoroutine(BlinkEffectCoroutine(lowHealthEffect));
                    _blinkingCoroutines[clientId] = coroutine;
                    _activeLowHealthEffects[clientId] = lowHealthEffect;
                }
            }
            else
            {
                if (_blinkingCoroutines.TryGetValue(clientId, out var coroutine)) StopCoroutine(coroutine);
                _blinkingCoroutines.Remove(clientId);
                if (_activeLowHealthEffects.TryGetValue(clientId, out var effect)) effect.SetOpacity(0f);
                _activeLowHealthEffects.Remove(clientId);
            }

            if (clientId == NetworkManager.Singleton.LocalClientId && MusicManager.Instance != null)
            {
                if (isLowOxygen) MusicManager.Instance.SetPitch(_lowOxygenPitch);
                else MusicManager.Instance.ResetPitch();
            }
        }

        private System.Collections.IEnumerator BlinkEffectCoroutine(LowHealthEffect effect)
        {
            while (true)
            {
                float t = 0;
                while (t < 1) { t += Time.deltaTime * 2; effect.SetOpacity(Mathf.Lerp(0, 1f, t)); yield return null; }
                t = 0;
                while (t < 1) { t += Time.deltaTime * 2; effect.SetOpacity(Mathf.Lerp(1f, 0, t)); yield return null; }
            }
        }

        private void ReturnToMainMenu()
        {
            NetworkManager.Singleton.Shutdown();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        #endregion
    }
}
