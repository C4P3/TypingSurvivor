using UnityEngine;
using TMPro;
using System;
using TypingSurvivor.Features.UI.Common;

namespace TypingSurvivor.Features.UI.Screens
{
    /// <summary>
    /// Displays the game result and provides options for rematch or returning to the main menu.
    /// Inherits from ScreenBase to get fade in/out functionality.
    /// </summary>
    public class ResultScreen : ScreenBase
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private InteractiveButton _rematchButton;
        [SerializeField] private InteractiveButton _mainMenuButton;

        public event Action OnRematchClicked;
        public event Action OnMainMenuClicked;

        protected override void Awake()
        {
            base.Awake();
            _rematchButton.onClick.AddListener(() => OnRematchClicked?.Invoke());
            _mainMenuButton.onClick.AddListener(() => OnMainMenuClicked?.Invoke());
            
            // Start hidden
            _canvasGroup.alpha = 0;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private void OnDestroy()
        {
            _rematchButton.onClick.RemoveAllListeners();
            _mainMenuButton.onClick.RemoveAllListeners();
        }

        // Show method is now overloaded to accept the result message
        public void Show(string resultMessage)
        {
            _resultText.text = resultMessage;
            base.Show(); // Call the base class Show to trigger the fade-in
        }
    }
}
