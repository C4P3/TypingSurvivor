
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

            // Use the new method to get both ID and display name
            var profiles = _appManager.AuthService.GetProfileDisplayData();
            if (profiles == null) return;

            foreach (var profile in profiles)
            {
                GameObject buttonGO = Instantiate(_profileButtonPrefab, _profileListContainer);
                var profileButton = buttonGO.GetComponent<ProfileListItem>();
                if (profileButton != null)
                {
                    // Pass the display name for the button text, and the ID for the callback
                    profileButton.Initialize(profile.Value, OnProfileSelected, profile.Key);
                }
            }
        }

        private async void OnProfileSelected(string profileId)
        {
            bool success = await _appManager.AuthService.SwitchProfileAndSignInAsync(profileId);
            if (success)
            {
                // Reload player data for the new profile
                var playerData = await _appManager.CloudSaveService.LoadPlayerDataAsync();
                _appManager.CachedPlayerData = playerData;

                // After loading data, update the display name cache
                await _appManager.PostSignInProcessAsync();

                // Apply the loaded settings for the new profile
                if (TypingSurvivor.Features.Core.Settings.SettingsManager.Instance != null)
                {
                    TypingSurvivor.Features.Core.Settings.SettingsManager.Instance.LoadSettings(playerData?.Settings);
                }

                // Also, re-fetch the rank for the new profile
                if (_appManager.SurvivalLeaderboardService != null)
                {
                    _appManager.CachedRankData = await _appManager.SurvivalLeaderboardService.GetPlayerRankAsync();
                }
                
                // Proceed to the main menu
                _flowCoordinator.RequestStateChange(UIFlowCoordinator.PlayerUIState.InMainMenu);
            }
            else
            {
                // Handle switch failure (e.g., show an error message)
                Debug.LogError($"Failed to switch to profile: {profileId}");
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
