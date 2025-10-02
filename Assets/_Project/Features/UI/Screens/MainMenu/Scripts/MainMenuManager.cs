using UnityEngine;
using TMPro;
using TypingSurvivor.Features.Core.App;
using TypingSurvivor.Features.Core.Audio;
using TypingSurvivor.Features.UI.Common;
using TypingSurvivor.Features.Core.Matchmaking;
using System.Threading.Tasks;

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

        [Header("Title Screen UI")]
        [SerializeField] private InteractiveButton _titleScreenButton;
        [SerializeField] private TMP_Text _titleStatusText;

        [Header("Main Menu Buttons")]
        [SerializeField] private InteractiveButton _singlePlayerButton;
        [SerializeField] private InteractiveButton _multiplayerButton;
        [SerializeField] private InteractiveButton _rankingButton;
        [SerializeField] private InteractiveButton _settingsButton;
        [SerializeField] private InteractiveButton _shopButton;
        [SerializeField] private InteractiveButton _creditsButton;

        [Header("Level Select Buttons")]
        [SerializeField] private InteractiveButton _difficultyEasyButton;
        [SerializeField] private InteractiveButton _closeLevelSelectButton;

        [Header("Multiplayer Buttons")]
        [SerializeField] private InteractiveButton _freeMatchButton;
        [SerializeField] private InteractiveButton _privateMatchButton;
        [SerializeField] private InteractiveButton _closeMultiplayerModeSelectButton;
        [SerializeField] private InteractiveButton _cancelMatchmakingButton;

        [Header("Private Match UI")]
        [SerializeField] private TMP_InputField _roomCodeInput;
        [SerializeField] private InteractiveButton _joinPrivateMatchButton;

        private bool _isSigningIn;

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
            // ... (other listeners are unchanged) ...

            // --- Initial State ---
            _titleScreenButton.interactable = false;
            if (_titleStatusText != null) _titleStatusText.text = "Connecting...";
            MusicManager.Instance.Play(SoundId.MainMenuMusic, 0f);
            _uiManager.ShowScreen(_titleScreen);

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
            _titleScreenButton.onClick.RemoveListener(TitleScreen_OnClick);
            // ... (remove other listeners) ...
        }

        private void HandleCoreServicesInitialized()
        {
            // Initialize other controllers that depend on core services
            var appManager = AppManager.Instance;
            var matchmakingService = appManager.MatchmakingService;
            if (matchmakingService != null && _matchmakingController != null)
            {
                _matchmakingController.Initialize(matchmakingService, _uiManager, appManager);
            }

            // Now, attempt to sign in automatically
            _ = TrySignInAsync();
        }

        private async Task TrySignInAsync()
        {
            if (_isSigningIn) return;
            _isSigningIn = true;
            _titleScreenButton.interactable = false;

            if (_titleStatusText != null) _titleStatusText.text = "Signing In...";

            bool success = await AppManager.Instance.AuthService.SignInAnonymouslyAsync();

            if (success)
            {
                if (_titleStatusText != null) _titleStatusText.text = "Click to Start";
                _titleScreenButton.interactable = true;
            }
            else
            {
                if (_titleStatusText != null) _titleStatusText.text = "Sign-In Failed. Click to Retry.";
                _titleScreenButton.interactable = true;
            }

            _isSigningIn = false;
        }

        private void TitleScreen_OnClick()
        {
            if (AppManager.Instance.AuthService.IsSignedIn)
            {
                if (!AssertScreenIsAssigned(_mainMenuScreen, "Main Menu Screen")) return;
                _uiManager.ShowScreen(_mainMenuScreen);
            }
            else
            {
                _ = TrySignInAsync();
            }
        }

        private void SinglePlayerButton_OnClick() 
        {
            if (!AssertScreenIsAssigned(_levelSelectPanel, "Level Select Panel")) return;
            _uiManager.PushPanel(_levelSelectPanel);
        }
        private void MultiplayerButton_OnClick() 
        {
            if (!AssertScreenIsAssigned(_multiplayerModeSelectPanel, "Multiplayer Mode Select Panel")) return;
            _uiManager.ShowScreen(_multiplayerModeSelectPanel);
        }
        private void RankingButton_OnClick() 
        {
            if (!AssertScreenIsAssigned(_rankingScreen, "Ranking Screen")) return;
            _uiManager.ShowScreen(_rankingScreen);
        }
        private void SettingsButton_OnClick() 
        {
            if (!AssertScreenIsAssigned(_settingsScreen, "Settings Screen")) return;
            _uiManager.ShowScreen(_settingsScreen);
        }
        private void ShopButton_OnClick() 
        {
            if (!AssertScreenIsAssigned(_shopScreen, "Shop Screen")) return;
            _uiManager.ShowScreen(_shopScreen);
        }
        private void CreditsButton_OnClick() 
        {
            if (!AssertScreenIsAssigned(_creditsScreen, "Credits Screen")) return;
            _uiManager.ShowScreen(_creditsScreen);
        }
        private void GoToMainMenu_OnClick() 
        {
            if (!AssertScreenIsAssigned(_mainMenuScreen, "Main Menu Screen")) return;
            _uiManager.ShowScreen(_mainMenuScreen);
        }
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
            if (!AssertScreenIsAssigned(_roomCodePanel, "Room Code Panel")) return;
            _uiManager.PushPanel(_roomCodePanel);
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

        private bool AssertScreenIsAssigned(ScreenBase screen, string screenName)
        {
            if (screen == null)
            {
                Debug.LogError($"The '{screenName}' has not been assigned in the MainMenuManager's inspector. Please assign the correct UI panel GameObject.");
                return false;
            }
            return true;
        }
    }
}
