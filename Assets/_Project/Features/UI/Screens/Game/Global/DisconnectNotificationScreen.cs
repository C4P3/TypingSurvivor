
using System;
using TMPro;
using TypingSurvivor.Features.UI.Common;
using UnityEngine;

namespace TypingSurvivor.Features.UI.Screens.Game.Global
{
    public class DisconnectNotificationScreen : ScreenBase
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private TextMeshProUGUI _buttonText;
        [SerializeField] private InteractiveButton _actionButton;

        private Action _onActionCallback;

        protected override void Awake()
        {
            base.Awake();
            if (_actionButton != null)
            {
                _actionButton.onClick.AddListener(() => _onActionCallback?.Invoke());
            }
        }

        public void Show(string message, string buttonText, Action onAction)
        {
            if (_messageText != null) _messageText.text = message;
            if (_buttonText != null) _buttonText.text = buttonText;
            _onActionCallback = onAction;

            base.Show();
        }
    }
}
