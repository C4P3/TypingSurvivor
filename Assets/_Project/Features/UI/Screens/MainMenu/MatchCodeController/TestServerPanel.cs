
using UnityEngine;
using TMPro;
using TypingSurvivor.Features.UI.Common;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    public class TestServerPanel : ScreenBase
    {
        [SerializeField] private TMP_InputField _ipAddressInput;
        [SerializeField] private TMP_InputField _portInput;
        [SerializeField] private InteractiveButton _clientButton;
        [SerializeField] private InteractiveButton _serverButton;
        [SerializeField] private InteractiveButton _backButton;
        [SerializeField] private TMP_Text _errorText;

        private UIFlowCoordinator _flowCoordinator;

        public void Initialize(UIFlowCoordinator coordinator)
        {
            _flowCoordinator = coordinator;
            _clientButton.onClick.AddListener(OnClientButtonClicked);
            _serverButton.onClick.AddListener(OnServerButtonClicked);
            _backButton.onClick.AddListener(OnBackButtonClicked);
        }

        public override void Show()
        {
            base.Show();
            if (_errorText != null) _errorText.gameObject.SetActive(false);
            // Set default values from OnGUI
            _ipAddressInput.text = "127.0.0.1";
            _portInput.text = "7777";
        }

        private void OnClientButtonClicked()
        {
            if (ushort.TryParse(_portInput.text, out ushort port))
            {
                // The actual logic will be in UIFlowCoordinator
                _flowCoordinator.StartTestClient(_ipAddressInput.text, port);
            }
            else
            {
                ShowError("Invalid Port Number!");
            }
        }

        private void OnServerButtonClicked()
        {
            if (ushort.TryParse(_portInput.text, out ushort port))
            {
                // The actual logic will be in UIFlowCoordinator
                _flowCoordinator.StartTestServer(_ipAddressInput.text, port);
            }
            else
            {
                ShowError("Invalid Port Number!");
            }
        }

        private void OnBackButtonClicked()
        {
            _flowCoordinator.CloseCurrentPanel();
        }

        private void ShowError(string message)
        {
            if (_errorText == null) return;
            _errorText.text = message;
            _errorText.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            _clientButton.onClick.RemoveAllListeners();
            _serverButton.onClick.RemoveAllListeners();
            _backButton.onClick.RemoveAllListeners();
        }
    }
}
