using System;
using TypingSurvivor.Features.Core.CloudSave;
using UnityEngine;
using UnityEngine.InputSystem;
using GameControlsInput;
using TypingSurvivor.Features.Core.App; // Add this using directive

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
            DontDestroyOnLoad(gameObject);

            Settings = new PlayerSettingsData();
            _gameControls = new GameControls();
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

        public void SaveKeybindings()
        {
            Settings.KeybindingsOverrideJson = _gameControls.SaveBindingOverridesAsJson();
            Debug.Log("Keybinding overrides saved.");
            
            // Trigger the actual cloud save
            if (AppManager.Instance != null && AppManager.Instance.CloudSaveService != null && AppManager.Instance.CachedPlayerData != null)
            {
                AppManager.Instance.CloudSaveService.SavePlayerDataAsync(AppManager.Instance.CachedPlayerData);
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
                    operation.Dispose();
                    _gameControls.Enable();
                    onComplete?.Invoke(true);
                    SaveKeybindings(); // Automatically save after a successful rebind
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
