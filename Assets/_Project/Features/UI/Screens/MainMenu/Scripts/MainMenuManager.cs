using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TypingSurvivor.Features.Core.App;
using TypingSurvivor.Features.Core.Auth;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    /// <summary>
    /// Manages the main menu UI, including the sign-in process.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [SerializeField] private Button _signInButton;
        [SerializeField] private TextMeshProUGUI _statusText;

        private IAuthenticationService _authService;

        private void Start()
        {
            // AppManagerから認証サービスへの参照を取得
            _authService = AppManager.Instance.AuthService;

            _signInButton.onClick.AddListener(SignInButton_OnClick);
            _statusText.text = "Please sign in.";
        }

        private void OnDestroy()
        {
            // リスナーをクリーンアップ
            _signInButton.onClick.RemoveListener(SignInButton_OnClick);
        }

        private async void SignInButton_OnClick()
        {
            _signInButton.interactable = false;
            _statusText.text = "Signing in...";

            bool success = await _authService.SignInAnonymouslyAsync();

            if (success)
            {
                _statusText.text = "Sign-in successful!";
                // TODO: Proceed to the next scene or enable other UI elements
            }
            else
            {
                _statusText.text = "Sign-in failed. Please try again.";
                _signInButton.interactable = true;
            }
        }
    }
}
