using System;
using TypingSurvivor.Features.Core.CloudSave;
using UnityEngine;
using UnityEngine.InputSystem;
using GameControlsInput;
using TypingSurvivor.Features.Core.App;
using System.Threading.Tasks; // Add this using directive

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
                    Debug.Log("Loaded settings from local cache.");
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
                Debug.Log("No local cache found. Initializing with default settings.");
            }
        }

        public void LoadSettings(PlayerSettingsData settings)
        {
            Settings = settings ?? new PlayerSettingsData();
            
            OnBgmVolumeChanged?.Invoke(Settings.BgmVolume);
            OnSfxVolumeChanged?.Invoke(Settings.SfxVolume);

            ApplyKeybindings();
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

        public async Task<bool> SaveAllSettings()
        {
            Settings.KeybindingsOverrideJson = _gameControls.SaveBindingOverridesAsJson();
            Debug.Log("Attempting to save settings to cloud...");

            if (AppManager.Instance?.CloudSaveService != null && AppManager.Instance.CachedPlayerData != null)
            {
                bool success = await AppManager.Instance.CloudSaveService.SavePlayerDataAsync(AppManager.Instance.CachedPlayerData);
                if (success)
                {
                    Debug.Log("Cloud save successful. Updating local cache.");
                    SaveSettingsToLocalCache();
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
