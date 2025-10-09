using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TypingSurvivor.Features.UI.Common
{
    public class NotificationPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private InteractiveButton _actionButton;
        [SerializeField] private TextMeshProUGUI _buttonText;

        private Action _onActionCallback;

        private void Awake()
        {
            if (_actionButton != null)
            {
                _actionButton.onClick.AddListener(() =>
                {
                    _onActionCallback?.Invoke();
                    Destroy(gameObject);
                });
            }
        }

        public void Show(string message, float? duration = null, string buttonLabel = null, Action onAction = null)
        {
            if (_messageText != null)
            {
                _messageText.text = message;
            }

            _onActionCallback = onAction;

            if (_actionButton != null)
            {
                bool hasAction = !string.IsNullOrEmpty(buttonLabel) && onAction != null;
                _actionButton.gameObject.SetActive(hasAction);
                if (hasAction && _buttonText != null)
                {
                    _buttonText.text = buttonLabel;
                }
            }

            if (duration.HasValue)
            {
                StartCoroutine(HideAfterDelay(duration.Value));
            }
        }

        private IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(gameObject);
        }
    }
}
