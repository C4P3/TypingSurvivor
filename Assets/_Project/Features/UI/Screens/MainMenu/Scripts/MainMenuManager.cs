using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TypingSurvivor.Features.Core.App;
using TypingSurvivor.Features.Core.Auth;
using TypingSurvivor.Features.Core.Audio;
using Unity.Netcode;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    /// <summary>
    /// Manages the main menu UI, including the sign-in process and game start options.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Authentication")]
        [SerializeField] private Button _signInButton;
        [SerializeField] private TextMeshProUGUI _statusText;

        [Header("Game Start")]
        [SerializeField] private GameObject _gameStartPanel; // Panel containing the buttons below
        [SerializeField] private Button _startSinglePlayerButton;
        [SerializeField] private Button _joinClientButton;
        [SerializeField] private Button _startServerButton; // For debug

        private IAuthenticationService _authService;
        private const string GameSceneName = "Game";

        private void Start()
        {
            _gameStartPanel.SetActive(false);
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

        /// <summary>
        /// Called when AppManager has finished initializing core services.
        /// </summary>
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
                _signInButton.gameObject.SetActive(false);
                _gameStartPanel.SetActive(true); // Show game start options
            }
            else
            {
                _statusText.text = "Sign-in failed. Please try again.";
                _signInButton.interactable = true;
            }
        }

        private void StartSinglePlayerButton_OnClick()
        {
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;
            
            MusicManager.Instance.Stop(0f);
            AppManager.Instance.SetGameMode(GameModeType.SinglePlayer);
            
            if (NetworkManager.Singleton.StartHost())
            {
                NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }

        private void JoinClientButton_OnClick()
        {
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;

            MusicManager.Instance.Stop(0f);
            AppManager.Instance.SetGameMode(GameModeType.MultiPlayer);

            const string ipAddress = "127.0.0.1";
            const ushort port = 7777;

            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().SetConnectionData(ipAddress, port);
            NetworkManager.Singleton.StartClient();
        }

        private void StartServerButton_OnClick()
        {
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;

            MusicManager.Instance.Stop(0f);
            AppManager.Instance.SetGameMode(GameModeType.MultiPlayer);

            if (NetworkManager.Singleton.StartServer())
            {
                NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }
    }
}
