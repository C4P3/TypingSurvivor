using UnityEngine;
using TMPro;
using TypingSurvivor.Features.Core.App;
using TypingSurvivor.Features.Core.Auth;
using TypingSurvivor.Features.Core.Audio;
using TypingSurvivor.Features.UI.Common;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    /// <summary>
    /// Manages the main menu UI, including screen and panel transitions.
    /// </summary>
    [RequireComponent(typeof(UIManager))]
    public class MainMenuManager : MonoBehaviour
    {
        [Header("UI System")]
        [SerializeField] private UIManager _uiManager;

        [Header("Screens")]
        [SerializeField] private ScreenBase _titleScreen; // Assuming a title screen exists
        [SerializeField] private ScreenBase _mainMenuScreen;
        [SerializeField] private ScreenBase _levelSelectPanel;
        [SerializeField] private ScreenBase _multiplayerModeSelectPanel;
        [SerializeField] private ScreenBase _matchmakingPanel;

        [Header("Title Screen")]
        [SerializeField] private InteractiveButton _titleScreenButton; // Button to proceed from title

        [Header("Main Menu Screen")]
        [SerializeField] private InteractiveButton _singlePlayerButton;
        [SerializeField] private InteractiveButton _multiplayerButton;
        // TODO: Add buttons for Ranking, Settings, Shop, Credits

        [Header("Level Select Panel")]
        [SerializeField] private InteractiveButton _difficultyEasyButton;
        [SerializeField] private InteractiveButton _closeLevelSelectButton;

        [Header("Multiplayer Mode Select Panel")]
        [SerializeField] private InteractiveButton _freeMatchButton;
        [SerializeField] private InteractiveButton _closeMultiplayerModeSelectButton;

        [Header("Matchmaking Panel")]
        [SerializeField] private InteractiveButton _cancelMatchmakingButton;

        private void Awake()
        {
            if (_uiManager == null) _uiManager = GetComponent<UIManager>();
        }

        private void Start()
        {
            // --- Event Listeners ---
            // Title
            _titleScreenButton.onClick.AddListener(TitleScreen_OnClick);
            // Main Menu
            _singlePlayerButton.onClick.AddListener(SinglePlayerButton_OnClick);
            _multiplayerButton.onClick.AddListener(MultiplayerButton_OnClick);
            // Level Select
            _difficultyEasyButton.onClick.AddListener(DifficultyEasyButton_OnClick);
            _closeLevelSelectButton.onClick.AddListener(CloseTopOverlay_OnClick);
            // Multiplayer Select
            _freeMatchButton.onClick.AddListener(FreeMatchButton_OnClick);
            _closeMultiplayerModeSelectButton.onClick.AddListener(CloseTopOverlay_OnClick);
            // Matchmaking
            _cancelMatchmakingButton.onClick.AddListener(CloseTopOverlay_OnClick);

            // --- Initial State ---
            MusicManager.Instance.Play(SoundId.MainMenuMusic, 0f);
            // Assuming anonymous sign-in happens automatically on App start or is handled elsewhere.
            _uiManager.ShowScreen(_titleScreen);
        }

        private void OnDestroy()
        {
            // Unsubscribe from all events to prevent memory leaks
        }

        // --- OnClick Handlers ---

        private void TitleScreen_OnClick()
        {
            _uiManager.ShowScreen(_mainMenuScreen);
        }

        private void SinglePlayerButton_OnClick()
        {
            _uiManager.ShowPanelOverlay(_levelSelectPanel);
        }

        private void MultiplayerButton_OnClick()
        {
            _uiManager.ShowPanelOverlay(_multiplayerModeSelectPanel);
        }

        private void DifficultyEasyButton_OnClick()
        {
            // TODO: Set difficulty level in a service
            AppManager.Instance.StartHost(GameModeType.SinglePlayer);
        }

        private void FreeMatchButton_OnClick()
        {
            _uiManager.ShowPanelOverlay(_matchmakingPanel);
            // TODO: Call matchmaking service
        }

        private void CloseTopOverlay_OnClick()
        {
            _uiManager.HideTopOverlay();
        }
    }
}
