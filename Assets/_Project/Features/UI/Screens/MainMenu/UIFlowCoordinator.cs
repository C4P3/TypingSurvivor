using UnityEngine;
using System.Threading.Tasks;
using TypingSurvivor.Features.Core.App;
using TypingSurvivor.Features.Core.Audio;
using TypingSurvivor.Features.UI.Common;
using TypingSurvivor.Features.Core.Audio.Data;

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
        [SerializeField] private ScreenBase _howToPlayScreen;
        [SerializeField] private ScreenBase _rankingScreen;
        [SerializeField] private ScreenBase _shopScreen;
        [SerializeField] private ScreenBase _settingsScreen;
        
        [Header("Audio")]
        [SerializeField] private MusicData _titleMusic;
        [SerializeField] private MusicData _mainMenuMusic;

        // --- Screen-specific Controllers ---
        // 各パネルにアタッチされているコントローラーへの参照
        [Header("Screen-Specific Controllers")]
        [SerializeField] private TitleScreenController _titleScreenController;
        [SerializeField] private ProfileCreationController _profileCreationController;
        [SerializeField] private MainMenuController _mainMenuController;
        // 他の画面のコントローラーも必要に応じて追加

        private PlayerUIState _currentState;
        private bool _isInitialized = false;

        private void Start()
        {
            if (AppManager.Instance.IsCoreServicesInitialized)
            {
                HandleCoreServicesInitialized();
            }
            else
            {
                AppManager.Instance.OnCoreServicesInitialized += HandleCoreServicesInitialized;
            }
        }

        private void OnDestroy()
        {
            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnCoreServicesInitialized -= HandleCoreServicesInitialized;
            }
        }

        private void HandleCoreServicesInitialized()
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

            _matchmakingController.Initialize(AppManager.Instance.MatchmakingService, _uiManager, AppManager.Instance);

            RequestStateChange(PlayerUIState.SigningIn);
        }

        /// <summary>
        /// UIの状態を変更し、関連するUI表示を更新します。
        /// </summary>
        /// <param name="newState">遷移先の新しい状態</param>
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
                    _ = TrySignInAsync();
                    break;
                case PlayerUIState.SignInFailed:
                    _uiManager.ShowScreen(_titleScreen);
                    _titleScreen.UpdateView("Sign-In Failed. Click to Retry.", true);
                    break;
                case PlayerUIState.NeedsProfile:
                    _uiManager.ShowScreen(_profileCreationScreen);
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
                    // For unhandled states, default to the main menu.
                    Debug.LogWarning($"Unhandled UI state '{_currentState}', defaulting to MainMenu.");
                    _uiManager.ShowScreen(_mainMenuScreen);
                    break;
            }
        }

        private async Task TrySignInAsync()
        {
            // ログイン試行
            bool success = await AppManager.Instance.AuthService.SignInAnonymouslyAsync();

            if (success)
            {
                // プレイヤー名が登録されているかチェック (CloudSaveServiceにメソッドがある想定)
                // bool hasProfile = await AppManager.Instance.CloudSaveService.HasProfileData();
                bool hasProfile = false; // 仮実装：初回は必ず名前入力へ

                if (hasProfile)
                {
                    RequestStateChange(PlayerUIState.InMainMenu);
                }
                else
                {
                    RequestStateChange(PlayerUIState.NeedsProfile);
                }
            }
            else
            {
                RequestStateChange(PlayerUIState.SignInFailed);
            }
        }
        
        // --- 各コントローラーからの通知を受け取るメソッド ---

        /// <summary>
        /// タイトルスクリーンがクリックされたときに呼ばれます。
        /// </summary>
        public void OnTitleScreenAction()
        {
            // ログイン失敗時、または未ログイン状態なら再度ログインを試みる
            if (_currentState == PlayerUIState.SignInFailed || !AppManager.Instance.AuthService.IsSignedIn)
            {
                RequestStateChange(PlayerUIState.SigningIn);
            }
        }

        /// <summary>
        /// プロフィール作成が完了したときに呼ばれます。
        /// </summary>
        public void OnProfileCreated()
        {
            RequestStateChange(PlayerUIState.InMainMenu);
        }


        /// <summary>
        /// 現在のパネルを閉じて前の画面に戻るようUIManagerに要求します。
        /// </summary>
        public void CloseCurrentPanel()
        {
            _uiManager.PopPanel();
        }

        /// <summary>
        /// ゲームシーンを開始します。
        /// </summary>
        /// <param name="mode">ゲームモード</param>
        public void StartGame(GameModeType mode)
        {
            AppManager.Instance.StartGame(mode);
        }

        /// <summary>
        /// Starts public matchmaking.
        /// </summary>
        /// <param name="queueName">The name of the matchmaking queue to join.</param>
        public void StartPublicMatchmaking(string queueName)
        {
            RequestStateChange(PlayerUIState.WaitingInMatchmakingQueue);
            // _matchmakingController.StartPublicMatchmaking(queueName);
        }

        /// <summary>
        /// Starts private matchmaking by joining a room with a code.
        /// </summary>
        /// <param name="roomCode">The room code to join.</param>
        public void StartPrivateMatchmaking(string roomCode)
        {
            RequestStateChange(PlayerUIState.WaitingInMatchmakingQueue);
            // _matchmakingController.StartPrivateMatchmaking(roomCode);
        }
    }
}