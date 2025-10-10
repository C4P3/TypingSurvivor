using System;
using TMPro;
using TypingSurvivor.Features.Core.Settings;
using TypingSurvivor.Features.UI.Common;
using TypingSurvivor.Features.UI.Screens.MainMenu;
using UnityEngine;
using UnityEngine.UI;

namespace TypingSurvivor.Features.UI.Screens
{
    public class SettingsScreen : ScreenBase
    {
        [Header("Navigation")]
        [SerializeField] private InteractiveButton _backButton;

        [Header("Audio Settings")]
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;

        [Header("Keybinding Settings")]
        [SerializeField] private Button _moveUpButton;
        [SerializeField] private TextMeshProUGUI _moveUpText;
        [SerializeField] private Button _moveDownButton;
        [SerializeField] private TextMeshProUGUI _moveDownText;
        [SerializeField] private Button _moveLeftButton;
        [SerializeField] private TextMeshProUGUI _moveLeftText;
        [SerializeField] private Button _moveRightButton;
        [SerializeField] private TextMeshProUGUI _moveRightText;
        [SerializeField] private Button _interactButton;
        [SerializeField] private TextMeshProUGUI _interactText;

        [Header("Action Buttons")]
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _resetButton;

        private UIFlowCoordinator _flowCoordinator;
        private SettingsManager _settingsManager;

        public void Initialize(UIFlowCoordinator coordinator)
        {
            _flowCoordinator = coordinator;
            _settingsManager = SettingsManager.Instance;

            if (_settingsManager == null)
            {
                Debug.LogError("SettingsManager not found!");
                gameObject.SetActive(false);
                return;
            }

            SetupInitialValues();
            AddListeners();
        }

        private void OnBackButtonClicked()
        {
            // TODO: Add a confirmation dialog if there are unsaved changes.
            _settingsManager.SaveKeybindings(); // Autosave on back
            _flowCoordinator.RequestStateChange(UIFlowCoordinator.PlayerUIState.InMainMenu);
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }

        private void SetupInitialValues()
        {
            _bgmSlider.value = _settingsManager.Settings.BgmVolume;
            _sfxSlider.value = _settingsManager.Settings.SfxVolume;
            UpdateAllBindingDisplays();
        }

        private void AddListeners()
        {
            _backButton.onClick.AddListener(OnBackButtonClicked);
            _bgmSlider.onValueChanged.AddListener(_settingsManager.SetBgmVolume);
            _sfxSlider.onValueChanged.AddListener(_settingsManager.SetSfxVolume);

            _moveUpButton.onClick.AddListener(() => Rebind("Move", 1, _moveUpText));
            _moveDownButton.onClick.AddListener(() => Rebind("Move", 2, _moveDownText));
            _moveLeftButton.onClick.AddListener(() => Rebind("Move", 3, _moveLeftText));
            _moveRightButton.onClick.AddListener(() => Rebind("Move", 4, _moveRightText));
            _interactButton.onClick.AddListener(() => Rebind("MoveInteract", 0, _interactText));

            _resetButton.onClick.AddListener(OnResetButton);
            _saveButton.onClick.AddListener(OnSaveButton);
        }

        private void RemoveListeners()
        {
            if (_backButton != null) _backButton.onClick.RemoveListener(OnBackButtonClicked);
            if (_bgmSlider != null) _bgmSlider.onValueChanged.RemoveAllListeners();
            if (_sfxSlider != null) _sfxSlider.onValueChanged.RemoveAllListeners();
            
            _moveUpButton.onClick.RemoveAllListeners();
            _moveDownButton.onClick.RemoveAllListeners();
            _moveLeftButton.onClick.RemoveAllListeners();
            _moveRightButton.onClick.RemoveAllListeners();
            _interactButton.onClick.RemoveAllListeners();

            _resetButton.onClick.RemoveAllListeners();
            _saveButton.onClick.RemoveAllListeners();
        }

        private void UpdateAllBindingDisplays()
        {
            _moveUpText.text = GetBindingDisplayString("Move", 1);
            _moveDownText.text = GetBindingDisplayString("Move", 2);
            _moveLeftText.text = GetBindingDisplayString("Move", 3);
            _moveRightText.text = GetBindingDisplayString("Move", 4);
            _interactText.text = GetBindingDisplayString("MoveInteract", 0);
        }

        private string GetBindingDisplayString(string actionName, int bindingIndex)
        {
            var action = _settingsManager.SharedGameControls.asset.FindAction(actionName);
            if (action == null || bindingIndex >= action.bindings.Count)
            {
                return "N/A";
            }
            return action.bindings[bindingIndex].ToDisplayString();
        }

        private void Rebind(string actionName, int bindingIndex, TextMeshProUGUI displayText)
        {
            displayText.text = "..." ;
            SetAllButtonsInteractable(false);

            _settingsManager.PerformRebinding(actionName, bindingIndex, (success) =>
            {
                if (success)
                {
                    displayText.text = GetBindingDisplayString(actionName, bindingIndex);
                }
                else
                {
                    UpdateAllBindingDisplays(); // Revert to previous text if cancelled
                }
                SetAllButtonsInteractable(true);
            });
        }

        private void OnResetButton()
        {
            _settingsManager.ResetKeybindings();
            UpdateAllBindingDisplays();
        }

        private void OnSaveButton()
        {
            _settingsManager.SaveKeybindings();
            // Optionally, show a "Saved!" confirmation message
        }

        private void SetAllButtonsInteractable(bool interactable)
        {
            _moveUpButton.interactable = interactable;
            _moveDownButton.interactable = interactable;
            _moveLeftButton.interactable = interactable;
            _moveRightButton.interactable = interactable;
            _interactButton.interactable = interactable;
            _resetButton.interactable = interactable;
            _saveButton.interactable = interactable;
            _backButton.interactable = interactable;
        }
    }
}