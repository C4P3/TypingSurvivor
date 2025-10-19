using UnityEngine;
using System.Threading.Tasks;
using TypingSurvivor.Features.Core.App;
using TypingSurvivor.Features.Core.Audio;
using TypingSurvivor.Features.Core.CloudSave;
using TypingSurvivor.Features.Core.Leaderboard;
using TypingSurvivor.Features.Core.Matchmaking;
using TypingSurvivor.Features.UI.Common;
using TypingSurvivor.Features.UI.Screens;
using Unity.Netcode;
using System.Collections;
using TypingSurvivor.Features.Core.Settings;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    public class UIFlowCoordinator : MonoBehaviour
    {
        public enum PlayerUIState
        {
            Initializing,
            SigningIn,
            SignInFailed,
            OnTitle,
            NeedsProfile,
            InMainMenu,
            SelectingSinglePlayer,
            SelectingMultiplayer,
            InHowToPlay,
            InRanking,
            InShop,
            InSettings,
            EnteringMatchCode,
            InTestServerMenu,
            SelectingProfile // Added
        }

        [Header("UI System")]
        [SerializeField] private UIManager _uiManager;

        [Header("Screens & Panels")]
        [SerializeField] private TitleScreenController _titleScreen;
        [SerializeField] private ProfileCreationController _profileCreationScreen;
        [SerializeField] private ProfileSelectionController _profileSelectionScreen; // Added
        [SerializeField] private MainMenuController _mainMenuScreen;
        [SerializeField] private SinglePlayerSelectController _singlePlayerSelectScreen;
        [SerializeField] private MultiplayerSelectController _multiplayerSelectScreen;
        [SerializeField] private MatchCodeController _matchCodeScreen;
        [SerializeField] private HowToPlayScreen _howToPlayScreen;
        [SerializeField] private RankingScreen _rankingScreen;
        [SerializeField] private ShopScreen _shopScreen;
        [SerializeField] private SettingsScreen _settingsScreen;
        [SerializeField] private ConfirmationDialog _confirmationDialog;
        [SerializeField] private TestServerPanel _testServerPanel;

        [Header("Matchmaking Panels")]
        [SerializeField] private MatchmakingWaitPanel _rankedWaitPanel;
        [SerializeField] private MatchmakingWaitPanel _freeWaitPanel;
        [SerializeField] private MatchmakingWaitPanel _roomWaitPanel;
        
        // --- State ---
        private PlayerUIState _currentState;
        private bool _isInitialized = false;
        public bool HasProfile { get; private set; } = false;

        // --- Matchmaking State ---
        private MatchmakingService _matchmakingService;
        private GameModeType _currentGameMode;
        private MatchmakingWaitPanel _activeWaitPanel;

        private void Start()
        {
            if (AppManager.Instance.IsCoreServicesInitialized)
            {
                InitializeFlow();
            }
            else
            {
                AppManager.Instance.OnCoreServicesInitialized += InitializeFlow;
            }
        }

        private void OnDestroy()
        {
            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnCoreServicesInitialized -= InitializeFlow;
            }
            UnsubscribeFromMatchmakingEvents();
        }

        private void InitializeFlow()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            // Initialize all child controllers
            _titleScreen.Initialize(this);
            _profileCreationScreen.Initialize(this, AppManager.Instance);
            _profileSelectionScreen.Initialize(this, AppManager.Instance); // Added
            _mainMenuScreen.Initialize(this);
            _singlePlayerSelectScreen.Initialize(this);
            _multiplayerSelectScreen.Initialize(this);
            _matchCodeScreen.Initialize(this);
            _shopScreen.Initialize(this);
            _settingsScreen.Initialize(this);
            _rankingScreen.Initialize(this);
            _howToPlayScreen.Initialize(this);
            _confirmationDialog.Initialize(_uiManager);
            _testServerPanel.Initialize(this);
            
            // Initialize matchmaking
            _matchmakingService = AppManager.Instance.MatchmakingService;
            SubscribeToMatchmakingEvents();

            _ = CheckAuthenticationAndProceed();
        }

        private void SubscribeToMatchmakingEvents()
        {
            if (_matchmakingService == null) return;
            _matchmakingService.OnMatchSuccess += HandleMatchSuccess;
            _matchmakingService.OnMatchFailure += HandleMatchFailure;
            _matchmakingService.OnStatusUpdated += HandleStatusUpdate;

            if (_rankedWaitPanel != null) _rankedWaitPanel.OnCancelClicked += CancelMatchmaking;
            if (_freeWaitPanel != null) _freeWaitPanel.OnCancelClicked += CancelMatchmaking;
            if (_roomWaitPanel != null) _roomWaitPanel.OnCancelClicked += CancelMatchmaking;
        }

        private void UnsubscribeFromMatchmakingEvents()
        {
            if (_matchmakingService == null) return;
            _matchmakingService.OnMatchSuccess -= HandleMatchSuccess;
            _matchmakingService.OnMatchFailure -= HandleMatchFailure;
            _matchmakingService.OnStatusUpdated -= HandleStatusUpdate;

            if (_rankedWaitPanel != null) _rankedWaitPanel.OnCancelClicked -= CancelMatchmaking;
            if (_freeWaitPanel != null) _freeWaitPanel.OnCancelClicked -= CancelMatchmaking;
            if (_roomWaitPanel != null) _roomWaitPanel.OnCancelClicked -= CancelMatchmaking;
        }

        private async Task CheckAuthenticationAndProceed()
        {
            RequestStateChange(PlayerUIState.SigningIn);

            if (!AppManager.Instance.AuthService.IsSignedIn)
            {
                bool success = await AppManager.Instance.AuthService.SignInAnonymouslyAsync();
                if (!success)
                {
                    RequestStateChange(PlayerUIState.SignInFailed);
                    return;
                }
            }

            // Load and cache player data and rank info
            var appManager = AppManager.Instance;
            var playerData = await appManager.CloudSaveService.LoadPlayerDataAsync();
            appManager.CachedPlayerData = playerData;
            HasProfile = playerData != null && !string.IsNullOrWhiteSpace(playerData.PlayerName);

            // Apply loaded settings to the SettingsManager
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.LoadSettings(playerData?.Settings);
            }

            if (appManager.SurvivalLeaderboardService != null)
            {
                appManager.CachedRankData = await appManager.SurvivalLeaderboardService.GetPlayerRankAsync();
            }

            RequestStateChange(PlayerUIState.OnTitle);
        }

        public void RequestStateChange(PlayerUIState newState)
        {
            if (_currentState == newState && newState != PlayerUIState.OnTitle) return; // Avoid redundant state changes
            _currentState = newState;
            Debug.Log($"UI State changed to: {newState}");

            switch (_currentState)
            {
                case PlayerUIState.SigningIn:
                    MusicManager.Instance.Play(SoundId.TitleMusic, 0f);
                    _uiManager.ShowScreen(_titleScreen);
                    _titleScreen.UpdateView("Signing In...", false, false);
                    break;
                case PlayerUIState.SignInFailed:
                    _uiManager.ShowScreen(_titleScreen);
                    _titleScreen.UpdateView("Sign-In Failed. Click to Retry.", true, false);
                    break;
                case PlayerUIState.OnTitle:
                    _uiManager.ShowScreen(_titleScreen);
                    _titleScreen.UpdateView("Click to Start", true, true);
                    break;
                case PlayerUIState.NeedsProfile:
                    _uiManager.PushPanel(_profileCreationScreen);
                    break;
                case PlayerUIState.SelectingProfile: // Added
                    _uiManager.PushPanel(_profileSelectionScreen);
                    break;
                case PlayerUIState.InMainMenu:
                    MusicManager.Instance.Play(SoundId.MainMenuMusic, 0f);
                    _uiManager.ShowScreen(_mainMenuScreen);
                    break;
                case PlayerUIState.SelectingSinglePlayer:
                    _uiManager.PushPanel(_singlePlayerSelectScreen);
                    break;
                case PlayerUIState.SelectingMultiplayer:
                    _uiManager.PushPanel(_multiplayerSelectScreen);
                    break;
                case PlayerUIState.EnteringMatchCode:
                    _uiManager.PushPanel(_matchCodeScreen);
                    break;
                case PlayerUIState.InTestServerMenu:
                    _uiManager.PushPanel(_testServerPanel);
                    break;
                case PlayerUIState.InHowToPlay:
                    _uiManager.ShowScreen(_howToPlayScreen);
                    break;
                case PlayerUIState.InRanking:
                    _uiManager.ShowScreen(_rankingScreen);
                    break;
                case PlayerUIState.InShop:
                    _uiManager.ShowScreen(_shopScreen);
                    break;
                case PlayerUIState.InSettings:
                    _uiManager.ShowScreen(_settingsScreen);
                    break;
                default:
                    Debug.LogWarning($"Unhandled UI state '{_currentState}', defaulting to MainMenu.");
                    _uiManager.ShowScreen(_mainMenuScreen);
                    break;
            }
        }
        
        public void OnTitleScreenAction()
        {
            if (_currentState == PlayerUIState.SignInFailed)
            {
                _ = CheckAuthenticationAndProceed();
            }
            else if (AppManager.Instance.AuthService.IsSignedIn)
            {
                if (HasProfile)
                {
                    RequestStateChange(PlayerUIState.InMainMenu);
                }
                else
                {
                    RequestStateChange(PlayerUIState.NeedsProfile);
                }
            }
        }

        public void OnProfileCreated()
        {
            HasProfile = true;
            RequestStateChange(PlayerUIState.InMainMenu);
        }

        public void CloseCurrentPanel()
        {
            _uiManager.PopPanel();
        }

        public void StartGame(GameModeType mode)
        {
            AppManager.Instance.StartGame(mode);
        }

        public async Task StartPublicMatchmaking(string queueName, GameModeType gameMode)
        {
            _currentGameMode = gameMode;
            
            if (gameMode == GameModeType.RankedMatch)
            {
                _activeWaitPanel = _rankedWaitPanel;
            }
            else
            {
                _activeWaitPanel = _freeWaitPanel;
            }

            if (_activeWaitPanel == null)
            {
                Debug.LogError($"Wait panel for {gameMode} is not assigned in UIFlowCoordinator.");
                return;
            }

            _activeWaitPanel.PreparePanel(isPrivate: false);
            _activeWaitPanel.Show();
            
            await _matchmakingService.CreateTicketAsync(queueName);
        }

        public async Task StartPrivateMatchmaking(string roomCode)
        {
            _currentGameMode = GameModeType.MultiPlayer;
            _activeWaitPanel = _roomWaitPanel;

            if (_activeWaitPanel == null)
            {
                Debug.LogError("Room wait panel is not assigned in UIFlowCoordinator.");
                return;
            }
            if (string.IsNullOrEmpty(roomCode))
            {
                Debug.LogError("Room code cannot be empty for a private match.");
                return;
            }

            _activeWaitPanel.PreparePanel(isPrivate: true, roomCode: roomCode);
            _activeWaitPanel.Show();
            
            await _matchmakingService.CreateTicketAsync("free-match", roomCode);
        }

        public void CancelMatchmaking()
        {
            _matchmakingService.CancelMatchmaking();
        }

        private void HandleMatchSuccess(MatchmakingResult result)
        {
            if (_activeWaitPanel == null) return;
            _activeWaitPanel.UpdateStatus("Match Found! Connecting...");
            StartCoroutine(ConnectAfterDelay(result, 1.5f));
        }

        private IEnumerator ConnectAfterDelay(MatchmakingResult result, float delay)
        {
            yield return new WaitForSeconds(delay);
            Debug.Log($"Match found! Connecting to {result.Ip}:{result.Port}");
            AppManager.Instance.StartClient(result.Ip, (ushort)result.Port, _currentGameMode);
        }

        private void HandleMatchFailure(string reason)
        {
            if (_activeWaitPanel == null) return;
            _activeWaitPanel.UpdateStatus($"Failed: {reason}");
            StartCoroutine(ClosePanelAfterDelay(2.5f));
        }

        private IEnumerator ClosePanelAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_activeWaitPanel != null)
            {
                _activeWaitPanel.Hide();
            }
            _activeWaitPanel = null; // Clear active panel
        }

        private void HandleStatusUpdate(string status)
        {
            if (_activeWaitPanel == null) return;
            _activeWaitPanel.UpdateStatus(status);
            Debug.Log($"Matchmaking Status: {status}");
        }

    #region test用サーバー立ち上げ及び参加
        public void StartTestClient(string ip, ushort port)
        { 
            AppManager.Instance.StartClient(ip, port, GameModeType.MultiPlayer);
        }

        public void StartTestServer(string ip, ushort port)
        {
            AppManager.Instance.SetGameMode(GameModeType.MultiPlayer);
            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().SetConnectionData(ip, port);
            NetworkManager.Singleton.StartServer();
            NetworkManager.Singleton.SceneManager.LoadScene("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    #endregion
    }
}
