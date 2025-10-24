using System;
using TypingSurvivor.Features.Core.CloudSave;
using UnityEngine;
using UnityEngine.InputSystem;
using GameControlsInput;
using TypingSurvivor.Features.Core.App;
using System.Threading.Tasks;
using TypingSurvivor.Features.Core.Auth;
using Unity.Services.Authentication; // Add this using directive

namespace TypingSurvivor.Features.Core.Settings
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        public PlayerSettingsData Settings { get; private set; }
        private GameControls _gameControls;

        // --- Audio Events ---
        public event Action<float> OnBgmVolumeChanged;
        public event Action<float> OnSfxVolumeChanged;

        public GameControls SharedGameControls => _gameControls;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadSettingsFromLocalCache();
            _gameControls = new GameControls();
        }

        private void LoadSettingsFromLocalCache()
        {
            string json = PlayerPrefs.GetString("PlayerSettings", null);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    Settings = JsonUtility.FromJson<PlayerSettingsData>(json);
                    Debug.Log($"[SettingsManager] Loaded settings from LOCAL cache. BGM Volume: {Settings.BgmVolume}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load settings from local cache. Using defaults. Error: {e.Message}");
                    Settings = new PlayerSettingsData();
                }
            }
            else
            {
                Settings = new PlayerSettingsData();
                Debug.Log("[SettingsManager] No local cache found. Initializing with default settings.");
            }
        }

        public void LoadSettings(PlayerSettingsData settings)
        {
            Settings = settings ?? new PlayerSettingsData();

            string profileName = AppManager.Instance?.AuthService?.CurrentProfile ?? "Unknown";
            Debug.Log($"[SettingsManager] Applying settings from CLOUD for profile: '{profileName}'. BGM Volume: {Settings.BgmVolume}");

            OnBgmVolumeChanged?.Invoke(Settings.BgmVolume);
            OnSfxVolumeChanged?.Invoke(Settings.SfxVolume);

            ApplyKeybindings();

            // Also update the local cache so it reflects the settings of the last logged-in user
            SaveSettingsToLocalCache();
        }

        #region Audio
        public void SetBgmVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (Settings.BgmVolume != volume)
            {
                Settings.BgmVolume = volume;
                OnBgmVolumeChanged?.Invoke(volume);
            }
        }

        public void SetSfxVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (Settings.SfxVolume != volume)
            {
                Settings.SfxVolume = volume;
                OnSfxVolumeChanged?.Invoke(volume);
            }
        }
        #endregion

        #region Keybindings
        private void ApplyKeybindings()
        {
            if (string.IsNullOrEmpty(Settings.KeybindingsOverrideJson)) return;
            _gameControls.LoadBindingOverridesFromJson(Settings.KeybindingsOverrideJson);
            Debug.Log("Keybinding overrides applied.");
        }

        public async Task<bool> SaveAllSettingsAsync(string newPlayerName)
        {
            if (AppManager.Instance.CachedPlayerData == null)
            {
                Debug.LogError("Cannot save settings. CachedPlayerData is null. The user's data might be corrupted or missing.");
                return false;
            }

            // Part 1: Handle Name Change
            string oldPlayerName = AppManager.Instance.CachedPlayerData.PlayerName;
            bool isNameChanging = !string.IsNullOrWhiteSpace(newPlayerName) && oldPlayerName != newPlayerName;

            if (isNameChanging)
            {
                Debug.Log($"Attempting to change player name from '{oldPlayerName}' to '{newPlayerName}'.");

                // This updates the display name on the backend for services like Lobby
                await AuthenticationService.Instance.UpdatePlayerNameAsync(newPlayerName);
            
                // This updates the name in the cached data object
                AppManager.Instance.CachedPlayerData.PlayerName = newPlayerName;
                
                // Update the new display name cache here
                if (AppManager.Instance.AuthService is ClientAuthenticationService clientAuth)
                {
                    clientAuth.UpdateCachedDisplayName(clientAuth.CurrentProfile, newPlayerName);
                }
            }

            // Part 2: Handle Settings (Audio, Keys)
            Settings.KeybindingsOverrideJson = _gameControls.SaveBindingOverridesAsJson();
    
            // Update the settings object within the main player data cache
            if (AppManager.Instance.CachedPlayerData != null)
            {
                AppManager.Instance.CachedPlayerData.Settings = this.Settings;
            }
            else
            {
                Debug.LogError("CachedPlayerData is null. Cannot save settings.");
                return false;
            }

            // Part 3: Save Everything to Cloud and Local Cache
            Debug.Log("Attempting to save all data to cloud...");
            if (AppManager.Instance?.CloudSaveService != null)
            {
                bool success = await AppManager.Instance.CloudSaveService.SavePlayerDataAsync(AppManager.Instance.CachedPlayerData);
                if (success)
                {
                    Debug.Log("Cloud save successful. Updating local settings cache.");
                    SaveSettingsToLocalCache(); // This saves PlayerSettingsData to PlayerPrefs
                    return true;
                }
                else
                {
                    Debug.LogError("Cloud save failed. Local cache was not updated.");
                    return false;
                }
            }

            return false;
        }


        private void SaveSettingsToLocalCache()
        {
            try
            {
                string json = JsonUtility.ToJson(Settings);
                PlayerPrefs.SetString("PlayerSettings", json);
                PlayerPrefs.Save();
                Debug.Log("Settings saved to local cache.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save settings to local cache: {e.Message}");
            }
        }

        public void ResetKeybindings()
        {
            _gameControls.RemoveAllBindingOverrides();
            Settings.KeybindingsOverrideJson = null;
            Debug.Log("Keybinding overrides have been reset.");
        }

                public void PerformRebinding(string actionName, int bindingIndex, Action<bool> onComplete)
                {
                    var action = _gameControls.asset.FindAction(actionName);
                    if (action == null)
                    {
                        onComplete?.Invoke(false);
                        return;
                    }
        
                    _gameControls.Disable();
                    action.PerformInteractiveRebinding(bindingIndex)
                        .WithControlsExcluding("<Mouse>/leftButton")
                        .WithControlsExcluding("<Mouse>/rightButton")
                        .OnComplete(operation =>
                        {
                            // リバインド成功時に、現在のオーバーライド設定をSettingsオブジェクトに反映させる
                            Settings.KeybindingsOverrideJson = _gameControls.SaveBindingOverridesAsJson();
            
                            operation.Dispose();
                            _gameControls.Enable();
                            onComplete?.Invoke(true);
                        })
                        .OnCancel(operation =>
                        {
                            operation.Dispose();
                            _gameControls.Enable();
                            onComplete?.Invoke(false);
                        })
                        .Start();
                }
                #endregion
        

            }
        }
