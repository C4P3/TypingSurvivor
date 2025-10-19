
using System.Collections.Generic;
using UnityEngine;
using TypingSurvivor.Features.UI.Common;
using TypingSurvivor.Features.Core.App;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    /// <summary>
    /// Handles the logic for the profile selection screen.
    /// </summary>
    public class ProfileSelectionController : ScreenBase
    {
        [SerializeField] private InteractiveButton _createNewButton;
        [SerializeField] private InteractiveButton _backButton;
        [SerializeField] private RectTransform _profileListContainer;
        [SerializeField] private GameObject _profileButtonPrefab; // A prefab for the profile button

        private UIFlowCoordinator _flowCoordinator;
        private AppManager _appManager;

        public void Initialize(UIFlowCoordinator coordinator, AppManager appManager)
        {
            _flowCoordinator = coordinator;
            _appManager = appManager;

            _createNewButton.onClick.AddListener(OnCreateNewProfile);
            _backButton.onClick.AddListener(OnBack);
        }

        private void OnDestroy()
        {
            _createNewButton.onClick.RemoveListener(OnCreateNewProfile);
            _backButton.onClick.RemoveListener(OnBack);
        }

        public override void Show()
        {
            base.Show();
            PopulateProfileList();
        }

        private void PopulateProfileList()
        {
            // Clear existing buttons
            foreach (Transform child in _profileListContainer)
            {
                Destroy(child.gameObject);
            }

            IReadOnlyList<string> profiles = _appManager.AuthService.ListProfiles();

            foreach (string profileName in profiles)
            {
                GameObject buttonGO = Instantiate(_profileButtonPrefab, _profileListContainer);
                var profileButton = buttonGO.GetComponent<ProfileListItem>(); // Assuming the prefab has a ProfileListItem component
                if (profileButton != null)
                {
                    profileButton.Initialize(profileName, OnProfileSelected);
                }
            }
        }

        private async void OnProfileSelected(string profileName)
        {
            bool success = await _appManager.AuthService.SwitchProfileAndSignInAsync(profileName);
            if (success)
            {
                // Reload player data for the new profile
                var playerData = await _appManager.CloudSaveService.LoadPlayerDataAsync();
                _appManager.CachedPlayerData = playerData;
                
                // Proceed to the main menu
                _flowCoordinator.RequestStateChange(UIFlowCoordinator.PlayerUIState.InMainMenu);
            }
            else
            {
                // Handle switch failure (e.g., show an error message)
                Debug.LogError($"Failed to switch to profile: {profileName}");
            }
        }

        private void OnCreateNewProfile()
        {
            _flowCoordinator.RequestStateChange(UIFlowCoordinator.PlayerUIState.NeedsProfile);
        }

        private void OnBack()
        {
            _flowCoordinator.RequestStateChange(UIFlowCoordinator.PlayerUIState.OnTitle);
        }
    }
}
