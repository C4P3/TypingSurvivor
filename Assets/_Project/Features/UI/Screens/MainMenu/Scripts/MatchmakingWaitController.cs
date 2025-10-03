using TypingSurvivor.Features.UI.Common;
using TMPro;
using UnityEngine;
using System;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    public class MatchmakingWaitController : ScreenBase
    {
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

        public void UpdateStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status;
            }
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
                _cancelButton.onClick.RemoveAllListeners();
            }
        }
    }
}