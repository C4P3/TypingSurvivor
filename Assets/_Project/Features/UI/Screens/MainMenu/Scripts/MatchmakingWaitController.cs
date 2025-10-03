using TypingSurvivor.Features.UI.Common;
using TMPro;
using UnityEngine;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    /// <summary>
    /// Controls the UI for the matchmaking wait screen.
    /// Displays the current matchmaking status and handles cancellation.
    /// </summary>
    public class MatchmakingWaitController : ScreenBase
    {
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private InteractiveButton _cancelButton;

        private MatchmakingController _matchmakingController;

        public void Initialize(MatchmakingController matchmakingController)
        {
            _matchmakingController = matchmakingController;
            _cancelButton.onClick.AddListener(OnCancelButtonClicked);
        }

        /// <summary>
        /// Updates the status message displayed on the screen.
        /// </summary>
        /// <param name="status">The new status message.</param>
        public void UpdateStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status;
            }
        }

        private void OnCancelButtonClicked()
        {
            _cancelButton.interactable = false; // Prevent double clicks
            UpdateStatus("Cancelling...");
            _matchmakingController.Cancel();
        }

        public override void Show()
        {
            base.Show();
            _cancelButton.interactable = true; // Ensure button is usable when shown
        }

        private void OnDestroy()
        {
            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(OnCancelButtonClicked);
            }
        }
    }
}
