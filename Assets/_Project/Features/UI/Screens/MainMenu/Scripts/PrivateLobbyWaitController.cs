using TypingSurvivor.Features.UI.Common;
using TMPro;
using UnityEngine;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    /// <summary>
    /// Controls the UI for the private lobby waiting screen.
    /// Displays the room code and allows the host to cancel.
    /// </summary>
    public class PrivateLobbyWaitController : ScreenBase
    {
        [SerializeField] private TextMeshProUGUI _roomCodeText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private InteractiveButton _cancelButton;

        private MatchmakingController _matchmakingController;

        public void Initialize(MatchmakingController matchmakingController)
        {
            _matchmakingController = matchmakingController;
            _cancelButton.onClick.AddListener(OnCancelButtonClicked);
        }

        /// <summary>
        /// Shows the panel and displays the given room code.
        /// </summary>
        public void ShowWithRoomCode(string roomCode)
        {
            if (_roomCodeText != null)
            {
                _roomCodeText.text = $"Room Code: {roomCode}";
            }
            UpdateStatus("Waiting for players...");
            base.Show();
            _cancelButton.interactable = true;
        }
        
        public void UpdateStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status;
            }
        }

        private void OnCancelButtonClicked()
        {
            _cancelButton.interactable = false;
            UpdateStatus("Cancelling...");
            _matchmakingController.Cancel();
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
