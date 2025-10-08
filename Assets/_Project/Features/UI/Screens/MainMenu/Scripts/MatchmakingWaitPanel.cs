using TypingSurvivor.Features.UI.Common;
using TMPro;
using UnityEngine;
using System;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    public class MatchmakingWaitPanel : ScreenBase
    {
        [Header("Common Elements")]
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private InteractiveButton _cancelButton;

        [Header("Private Lobby (Optional)")]
        [SerializeField] private GameObject _roomCodeSection;
        [SerializeField] private TextMeshProUGUI _roomCodeText;

        public event Action OnCancelClicked;

        protected override void Awake()
        {
            base.Awake();
            _cancelButton?.onClick.AddListener(() => 
            {
                if(_cancelButton) _cancelButton.interactable = false;
                UpdateStatus("Cancelling...");
                OnCancelClicked?.Invoke();
            });
        }

        /// <summary>
        /// Prepares the panel's content before showing.
        /// </summary>
        /// <param name="isPrivate">Is this for a private lobby?</param>
        /// <param name="roomCode">The room code to display if it's a private lobby.</param>
        public void PreparePanel(bool isPrivate, string roomCode = "")
        {
            if (_roomCodeSection != null)
            {
                _roomCodeSection.SetActive(isPrivate);
            }

            if (isPrivate)
            {
                if (_roomCodeText != null) _roomCodeText.text = $"Room Code: {roomCode}";
                UpdateStatus("Waiting for players...");
            }
            else
            {
                UpdateStatus("Searching for a match...");
            }
        }

        public override void Show()
        {
            base.Show();
            // Ensure button is usable when the panel is shown by the UIManager
            if(_cancelButton) _cancelButton.interactable = true;
        }
        
        public void UpdateStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status;
            }
        }

        private void OnDestroy()
        {
            _cancelButton?.onClick.RemoveAllListeners();
        }
    }
}
