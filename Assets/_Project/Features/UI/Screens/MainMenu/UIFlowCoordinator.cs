using UnityEngine;
using System.Threading.Tasks;
using TypingSurvivor.Features.Core.App;
using TypingSurvivor.Features.Core.Audio;
using TypingSurvivor.Features.Core.CloudSave;
using TypingSurvivor.Features.Core.Leaderboard;
using TypingSurvivor.Features.UI.Common;
using TypingSurvivor.Features.UI.Screens;
using Unity.Netcode;

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
            EnteringMatchCode
        }

        [Header("UI System")]
        [SerializeField] private UIManager _uiManager;

        [Header("Controllers")]
        [SerializeField] private MatchmakingController _matchmakingController;

        [Header("Screens & Panels")]
        [SerializeField] private TitleScreenController _titleScreen;
        [SerializeField] private ProfileCreationController _profileCreationScreen;
        [SerializeField] private MainMenuController _mainMenuScreen;
        [SerializeField] private SinglePlayerSelectController _singlePlayerSelectScreen;
        [SerializeField] private MultiplayerSelectController _multiplayerSelectScreen;
        [SerializeField] private MatchCodeController _matchCodeScreen;
        [SerializeField] private HowToPlayScreen _howToPlayScreen;
        [SerializeField] private RankingScreen _rankingScreen;
        [SerializeField] private ShopScreen _shopScreen;
        [SerializeField] private SettingsScreen _settingsScreen;
        
        private PlayerUIState _currentState;
        private bool _isInitialized = false;
        private bool _hasProfile = false;

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
        }

        private void InitializeFlow()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            // Initialize all child controllers
            _titleScreen.Initialize(this);
            _profileCreationScreen.Initialize(this, AppManager.Instance);
            _mainMenuScreen.Initialize(this);
            _singlePlayerSelectScreen.Initialize(this);
            _multiplayerSelectScreen.Initialize(this);
            _matchCodeScreen.Initialize(this);
            _shopScreen.Initialize(this);
            _settingsScreen.Initialize(this);
            _rankingScreen.Initialize(this);
            _howToPlayScreen.Initialize(this);
            
            _matchmakingController.Initialize(AppManager.Instance.MatchmakingService, _uiManager, AppManager.Instance);

            _ = CheckAuthenticationAndProceed();
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
            _hasProfile = playerData != null && !string.IsNullOrWhiteSpace(playerData.PlayerName);

            if (appManager.SurvivalLeaderboardService != null)
            {
                appManager.CachedRankData = await appManager.SurvivalLeaderboardService.GetPlayerRankAsync();
            }

            RequestStateChange(PlayerUIState.OnTitle);
        }

        public void RequestStateChange(PlayerUIState newState)
        {
            _currentState = newState;
            Debug.Log($"UI State changed to: {newState}");

            switch (_currentState)
            {
                case PlayerUIState.SigningIn:
                    MusicManager.Instance.Play(SoundId.TitleMusic, 0f);
                    _uiManager.ShowScreen(_titleScreen);
                    _titleScreen.UpdateView("Signing In...", false);
                    break;
                case PlayerUIState.SignInFailed:
                    _uiManager.ShowScreen(_titleScreen);
                    _titleScreen.UpdateView("Sign-In Failed. Click to Retry.", true);
                    break;
                case PlayerUIState.OnTitle:
                    _uiManager.ShowScreen(_titleScreen);
                    _titleScreen.UpdateView("Click to Start", true);
                    break;
                case PlayerUIState.NeedsProfile:
                    _uiManager.PushPanel(_profileCreationScreen);
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
                return;
            }

            if (AppManager.Instance.AuthService.IsSignedIn)
            {
                if (_hasProfile)
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
            _hasProfile = true;
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
            await _matchmakingController.StartPublicMatchmaking(queueName, gameMode);
        }

        public async Task StartPrivateMatchmaking(string roomCode)
        {
            await _matchmakingController.StartPrivateMatchmaking(roomCode);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            // Only show the buttons if we are in the main menu and not connected
            if (_currentState == PlayerUIState.InMainMenu && !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                // Position the buttons in the top-left corner
                GUILayout.BeginArea(new Rect(10, 10, 200, 200));

                if (GUILayout.Button("Client (Ranked)"))
                {
                    AppManager.Instance.StartClient("127.0.0.1", 7777, GameModeType.RankedMatch);
                }

                if (GUILayout.Button("Client (Free Match)"))
                {
                    AppManager.Instance.StartClient("127.0.0.1", 7777, GameModeType.MultiPlayer);
                }

                if (GUILayout.Button("Server (Free Match)"))
                {
                    AppManager.Instance.SetGameMode(GameModeType.MultiPlayer);
                    NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().SetConnectionData("127.0.0.1", 7777);
                    NetworkManager.Singleton.StartServer();
                    NetworkManager.Singleton.SceneManager.LoadScene("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
                }

                if (GUILayout.Button("Server (Ranked)"))
                {
                    AppManager.Instance.SetGameMode(GameModeType.RankedMatch);
                    NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().SetConnectionData("127.0.0.1", 7777);
                    NetworkManager.Singleton.StartServer();
                    NetworkManager.Singleton.SceneManager.LoadScene("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
                }

                GUILayout.EndArea();
            }
        }
#endif
    }
}