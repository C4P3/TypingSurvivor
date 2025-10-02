using UnityEngine;
using TMPro;
using TypingSurvivor.Features.Core.App;
using TypingSurvivor.Features.Core.Audio;
using TypingSurvivor.Features.UI.Common;
using TypingSurvivor.Features.Core.Matchmaking;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    [RequireComponent(typeof(UIManager))]
    public class MainMenuManager : MonoBehaviour
    {
        [Header("UI System")]
        [SerializeField] private UIManager _uiManager;

        [Header("Controllers")]
        [SerializeField] private MatchmakingController _matchmakingController;

        [Header("Screens & Panels")]
        [SerializeField] private ScreenBase _titleScreen;
        [SerializeField] private ScreenBase _mainMenuScreen;
        [SerializeField] private ScreenBase _levelSelectPanel;
        [SerializeField] private ScreenBase _multiplayerModeSelectPanel;
        [SerializeField] private ScreenBase _roomCodePanel;
        [SerializeField] private ScreenBase _rankingScreen;
        [SerializeField] private ScreenBase _settingsScreen;
        [SerializeField] private ScreenBase _shopScreen;
        [SerializeField] private ScreenBase _creditsScreen;

        [Header("Buttons")]
        [SerializeField] private InteractiveButton _titleScreenButton;
        [SerializeField] private InteractiveButton _singlePlayerButton;
        [SerializeField] private InteractiveButton _multiplayerButton;
        [SerializeField] private InteractiveButton _rankingButton;
        [SerializeField] private InteractiveButton _settingsButton;
        [SerializeField] private InteractiveButton _shopButton;
        [SerializeField] private InteractiveButton _creditsButton;
        [SerializeField] private InteractiveButton _difficultyEasyButton;
        [SerializeField] private InteractiveButton _closeLevelSelectButton;
        [SerializeField] private InteractiveButton _freeMatchButton;
        [SerializeField] private InteractiveButton _privateMatchButton; // Button to open the room code panel
        [SerializeField] private InteractiveButton _closeMultiplayerModeSelectButton;
        [SerializeField] private InteractiveButton _cancelMatchmakingButton;

        [Header("Private Match UI")]
        [SerializeField] private TMP_InputField _roomCodeInput;
        [SerializeField] private InteractiveButton _joinPrivateMatchButton;

        private void Awake()
        {
            if (_uiManager == null) _uiManager = GetComponent<UIManager>();
        }

        private void Start()
        {
            // --- Event Listeners ---
            _titleScreenButton.onClick.AddListener(TitleScreen_OnClick);
            _singlePlayerButton.onClick.AddListener(SinglePlayerButton_OnClick);
            _multiplayerButton.onClick.AddListener(MultiplayerButton_OnClick);
            _rankingButton.onClick.AddListener(RankingButton_OnClick);
            _settingsButton.onClick.AddListener(SettingsButton_OnClick);
            _shopButton.onClick.AddListener(ShopButton_OnClick);
            _creditsButton.onClick.AddListener(CreditsButton_OnClick);
            _difficultyEasyButton.onClick.AddListener(DifficultyEasyButton_OnClick);
            _closeLevelSelectButton.onClick.AddListener(CloseTopOverlay_OnClick);
            _freeMatchButton.onClick.AddListener(PublicMatchmaking_OnClick);
            _privateMatchButton?.onClick.AddListener(PrivateMatchPanel_OnClick);
            _joinPrivateMatchButton?.onClick.AddListener(JoinPrivateMatch_OnClick);
            _closeMultiplayerModeSelectButton.onClick.AddListener(GoToMainMenu_OnClick);
            _cancelMatchmakingButton.onClick.AddListener(CancelMatchmaking_OnClick);

            // --- Initial State ---
            MusicManager.Instance.Play(SoundId.MainMenuMusic, 0f);
            _uiManager.ShowScreen(_titleScreen);

            if (AppManager.Instance.IsCoreServicesInitialized)
            {
                InitializeControllers();
            }
            else
            {
                AppManager.Instance.OnCoreServicesInitialized += InitializeControllers;
            }
        }

        private void OnDestroy()
        {
            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnCoreServicesInitialized -= InitializeControllers;
            }
        }

        private void InitializeControllers()
        {
            var appManager = AppManager.Instance;
            var matchmakingService = appManager.MatchmakingService;
            if (matchmakingService != null && _matchmakingController != null)
            {
                _matchmakingController.Initialize(matchmakingService, _uiManager, appManager);
            }
        }

        // --- OnClick Handlers ---

        private void TitleScreen_OnClick() => _uiManager.ShowScreen(_mainMenuScreen);
        private void SinglePlayerButton_OnClick() => _uiManager.PushPanel(_levelSelectPanel);
        private void MultiplayerButton_OnClick() => _uiManager.ShowScreen(_multiplayerModeSelectPanel);
        private void RankingButton_OnClick() => _uiManager.ShowScreen(_rankingScreen);
        private void SettingsButton_OnClick() => _uiManager.ShowScreen(_settingsScreen);
        private void ShopButton_OnClick() => _uiManager.ShowScreen(_shopScreen);
        private void CreditsButton_OnClick() => _uiManager.ShowScreen(_creditsScreen);
        private void GoToMainMenu_OnClick() => _uiManager.ShowScreen(_mainMenuScreen);
        private void CloseTopOverlay_OnClick() => _uiManager.PopPanel();

        private void DifficultyEasyButton_OnClick()
        {
            AppManager.Instance.StartHost(GameModeType.SinglePlayer);
        }

        private void PublicMatchmaking_OnClick()
        {
            _matchmakingController?.StartPublicMatchmaking("FreeMatchQueue");
        }

        private void PrivateMatchPanel_OnClick()
        {
            if (_roomCodePanel != null) _uiManager.PushPanel(_roomCodePanel);
        }

        private void JoinPrivateMatch_OnClick()
        {
            if (_roomCodeInput != null)
            {
                _matchmakingController?.StartPrivateMatch(_roomCodeInput.text);
            }
        }

        private void CancelMatchmaking_OnClick()
        {
            _matchmakingController?.Cancel();
        }
    }
}
