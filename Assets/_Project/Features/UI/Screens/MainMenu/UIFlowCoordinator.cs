using UnityEngine;
using System.Threading.Tasks;
using TypingSurvivor.Features.Core.App;
using TypingSurvivor.Features.Core.Audio;
using TypingSurvivor.Features.Core.CloudSave;
using TypingSurvivor.Features.UI.Common;
using TypingSurvivor.Features.UI.Screens;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    /// <summary>
    /// UIの状態遷移全体を管理する交通整理役。
    /// ログイン状態やプレイヤーのアクションに応じて、どの画面を表示するかを決定しUIManagerに指示します。
    /// </summary>
    public class UIFlowCoordinator : MonoBehaviour
    {
        // プレイヤーのUI状態を定義
        public enum PlayerUIState
        {
            Initializing,                // 初期化中
            SigningIn,                   // ログイン試行中
            SignInFailed,                // ログイン失敗
            OnTitle,                     // タイトル画面でクリック待ち
            NeedsProfile,                // 名前入力が必要
            InMainMenu,                  // メインメニュー表示中
            SelectingSinglePlayer,       // 一人で遊ぶ モード選択
            SelectingMultiplayer,        // みんなで遊ぶ モード選択
            InHowToPlay,                 // 遊び方
            InRanking,                   // ランキング
            InShop,                      // ショップ
            InSettings,                  // 設定
            EnteringMatchCode,           // 合言葉入力
            WaitingInMatchmakingQueue    // マッチング待機中
        }

        [Header("UI System")]
        [SerializeField] private UIManager _uiManager;

        [Header("Controllers")]
        [SerializeField] private MatchmakingController _matchmakingController;

        [Header("Screens & Panels")]
        [Header("Screen Controllers")]
        [SerializeField] private TitleScreenController _titleScreen;
        [SerializeField] private ProfileCreationController _profileCreationScreen;
        [SerializeField] private MainMenuController _mainMenuScreen;
        [SerializeField] private SinglePlayerSelectController _singlePlayerSelectScreen;
        [SerializeField] private MultiplayerSelectController _multiplayerSelectScreen;
        [SerializeField] private MatchCodeController _matchCodeScreen;
        [SerializeField] private ScreenBase _matchmakingWaitScreen;
        [SerializeField] private HowToPlayScreen _howToPlayScreen;
        [SerializeField] private RankingScreen _rankingScreen;
        [SerializeField] private ShopScreen _shopScreen;
        [SerializeField] private SettingsScreen _settingsScreen;
        
        // --- Screen-specific Controllers ---
        // 各パネルにアタッチされているコントローラーへの参照
        [Header("Screen-Specific Controllers")]
        [SerializeField] private TitleScreenController _titleScreenController;
        [SerializeField] private ProfileCreationController _profileCreationController;
        [SerializeField] private MainMenuController _mainMenuController;

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

            // 各画面コントローラーを初期化
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

            // _matchmakingController.Initialize(AppManager.Instance.MatchmakingService, _uiManager, AppManager.Instance);

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

            var playerData = await AppManager.Instance.CloudSaveService.LoadPlayerDataAsync();
            _hasProfile = playerData != null && !string.IsNullOrWhiteSpace(playerData.PlayerName);

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
                case PlayerUIState.WaitingInMatchmakingQueue:
                    _uiManager.PushPanel(_matchmakingWaitScreen);
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
            _hasProfile = true; // Update our cached status
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

        public void StartPublicMatchmaking(string queueName)
        {
            RequestStateChange(PlayerUIState.WaitingInMatchmakingQueue);
            // _matchmakingController.StartPublicMatchmaking(queueName);
        }

        public void StartPrivateMatchmaking(string roomCode)
        {
            RequestStateChange(PlayerUIState.WaitingInMatchmakingQueue);
            // _matchmakingController.StartPrivateMatchmaking(roomCode);
        }
    }
}
