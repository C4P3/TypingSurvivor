using UnityEngine;
using TMPro;
using TypingSurvivor.Features.Core.App;
using TypingSurvivor.Features.Core.Auth;
using TypingSurvivor.Features.Core.Audio;
using TypingSurvivor.Features.UI.Common;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    /// <summary>
    /// Manages the main menu UI, including the sign-in process and game start options.
    /// </summary>
    [RequireComponent(typeof(UIManager))]
    public class MainMenuManager : MonoBehaviour
    {
        [Header("UI System")]
        [SerializeField] private UIManager _uiManager;

        [Header("Screens")]
        [SerializeField] private ScreenBase _signInScreen;
        [SerializeField] private ScreenBase _gameStartScreen;

        [Header("Authentication")]
        [SerializeField] private InteractiveButton _signInButton;
        [SerializeField] private TextMeshProUGUI _statusText;

        [Header("Game Start")]
        [SerializeField] private InteractiveButton _startSinglePlayerButton;
        [SerializeField] private InteractiveButton _joinClientButton;
        [SerializeField] private InteractiveButton _startServerButton; // For debug

        private IAuthenticationService _authService;

        private void Awake()
        {
            if (_uiManager == null) _uiManager = GetComponent<UIManager>();
        }

        private void Start()
        {
            _signInButton.onClick.AddListener(SignInButton_OnClick);
            _startSinglePlayerButton.onClick.AddListener(StartSinglePlayerButton_OnClick);
            _joinClientButton.onClick.AddListener(JoinClientButton_OnClick);
            _startServerButton.onClick.AddListener(StartServerButton_OnClick);

            MusicManager.Instance.Play(SoundId.MainMenuMusic, 0f);

            // Check-then-Subscribe pattern
            if (AppManager.Instance.IsCoreServicesInitialized)
            {
                HandleCoreServicesInitialized();
            }
            else
            {
                _signInButton.interactable = false;
                _statusText.text = "Initializing...";
                AppManager.Instance.OnCoreServicesInitialized += HandleCoreServicesInitialized;
            }
            
            // Show the initial screen
            _uiManager.ShowScreen(_signInScreen);
        }

        private void OnDestroy()
        {
            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnCoreServicesInitialized -= HandleCoreServicesInitialized;
            }
            
            _signInButton.onClick.RemoveListener(SignInButton_OnClick);
            _startSinglePlayerButton.onClick.RemoveListener(StartSinglePlayerButton_OnClick);
            _joinClientButton.onClick.RemoveListener(JoinClientButton_OnClick);
            _startServerButton.onClick.RemoveListener(StartServerButton_OnClick);
        }

        private void HandleCoreServicesInitialized()
        {
            _authService = AppManager.Instance.AuthService;
            _signInButton.interactable = true;
            _statusText.text = "Please sign in.";
        }

        private async void SignInButton_OnClick()
        {
            _signInButton.interactable = false;
            _statusText.text = "Signing in...";

            bool success = await _authService.SignInAnonymouslyAsync();

            if (success)
            {
                _statusText.text = "Sign-in successful!";
                _uiManager.ShowScreen(_gameStartScreen);
            }
            else
            {
                _statusText.text = "Sign-in failed. Please try again.";
                _signInButton.interactable = true;
            }
        }

        private void StartSinglePlayerButton_OnClick()
        {
            AppManager.Instance.StartHost(GameModeType.SinglePlayer);
        }

        private void JoinClientButton_OnClick()
        {
            AppManager.Instance.StartClient();
        }

        private void StartServerButton_OnClick()
        {
            AppManager.Instance.StartServer();
        }
    }
}
