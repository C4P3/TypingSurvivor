using System;
using TMPro;
using TypingSurvivor.Features.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace TypingSurvivor.Features.UI.Common
{
    public class ConfirmationDialog : ScreenBase
    {
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _declineButton;
        [SerializeField] private Button _cancelButton;

        private UIManager _uiManager;

        public void Initialize(UIManager uiManager)
        {
            _uiManager = uiManager;
        }

        public void Show(string message, Action onConfirm, Action onDecline = null, Action onCancel = null)
        {
            _messageText.text = message;

            _confirmButton.onClick.RemoveAllListeners();
            _declineButton.onClick.RemoveAllListeners();
            _cancelButton.onClick.RemoveAllListeners();

            _confirmButton.onClick.AddListener(() => {
                onConfirm?.Invoke();
                _uiManager.PopPanel();
            });

            _declineButton.onClick.AddListener(() => {
                onDecline?.Invoke();
                _uiManager.PopPanel();
            });

            _cancelButton.onClick.AddListener(() => {
                onCancel?.Invoke();
                _uiManager.PopPanel();
            });

            _confirmButton.gameObject.SetActive(onConfirm != null);
            _declineButton.gameObject.SetActive(onDecline != null);
            _cancelButton.gameObject.SetActive(onCancel != null);

            _uiManager.PushPanel(this);
        }
    }
}
