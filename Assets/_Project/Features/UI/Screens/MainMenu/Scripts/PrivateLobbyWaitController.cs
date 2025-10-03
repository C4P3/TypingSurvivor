using TypingSurvivor.Features.UI.Common;
using TMPro;
using UnityEngine;
using System;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    public class PrivateLobbyWaitController : ScreenBase
    {
        [SerializeField] private TextMeshProUGUI _roomCodeText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private InteractiveButton _cancelButton;

        public event Action OnCancelClicked;

        protected override void Awake()
        {
            base.Awake();
            _cancelButton.onClick.AddListener(() => 
            {
                _cancelButton.interactable = false;
                UpdateStatus("Cancelling...");
                OnCancelClicked?.Invoke();
            });
        }

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

        private void OnDestroy()
        {
            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveAllListeners();
            }
        }
    }
}