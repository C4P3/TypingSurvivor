using UnityEngine;
using TMPro;
using TypingSurvivor.Features.UI.Common;
using TypingSurvivor.Features.Core.App;
using System.Threading.Tasks;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    /// <summary>
    /// プレイヤー名入力画面のロジックを担当します。ScreenBaseを継承して自身の表示/非表示を管理します。
    /// </summary>
    public class ProfileCreationController : ScreenBase
    {
        [SerializeField] private TMP_InputField _nameInputField;
        [SerializeField] private InteractiveButton _confirmButton;
        [SerializeField] private TMP_Text _errorText;

        private UIFlowCoordinator _flowCoordinator;
        private AppManager _appManager;

        protected override void Awake()
        {
            base.Awake();
            // この画面固有のAwake処理があればここに記述
        }

        /// <summary>
        /// UIFlowCoordinatorによって初期化されます。
        /// </summary>
        public void Initialize(UIFlowCoordinator coordinator, AppManager appManager)
        {
            _flowCoordinator = coordinator;
            _appManager = appManager;
            _confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            if(_errorText != null) _errorText.gameObject.SetActive(false);
        }
        
        private void OnDestroy()
        {
            _confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
        }

        private async void OnConfirmButtonClicked()
        {
            _confirmButton.interactable = false;
            if(_errorText != null) _errorText.gameObject.SetActive(false);

            string profileName = _nameInputField.text;

            // 簡単な入力値検証
            if (string.IsNullOrWhiteSpace(profileName) || profileName.Length < 3)
            {
                ShowError("Please enter at least 3 characters.");
                return;
            }

            // 1. Switch to the new profile and sign in. This will create the profile if it doesn't exist.
            bool signInSuccess = await _appManager.AuthService.SwitchProfileAndSignInAsync(profileName);

            if (!signInSuccess)
            {
                ShowError("Failed to create profile. Please try again.");
                return;
            }

            // 2. Update the player name for the new profile.
            // Note: UGS PlayerName is different from Profile Name.
            await _appManager.AuthService.UpdatePlayerNameAsync(profileName);

            // 3. Save initial data for the new player.
            var newPlayerData = new Core.CloudSave.PlayerSaveData(profileName);
            bool saveDataSuccess = await _appManager.CloudSaveService.SavePlayerDataAsync(newPlayerData);

            if (saveDataSuccess)
            {
                // 4. Notify the flow coordinator that the profile is ready.
                _appManager.CachedPlayerData = newPlayerData;
                _flowCoordinator.OnProfileCreated();
            }
            else
            {
                ShowError("Failed to save initial data. Please try again.");
            }
        }

        private void ShowError(string message)
        {
            if (_errorText != null)
            {
                _errorText.text = message;
                _errorText.gameObject.SetActive(true);
            }
            _confirmButton.interactable = true;
        }
    }
}

