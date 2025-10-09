
using System.Collections;
using TMPro;
using UnityEngine;

namespace TypingSurvivor.Features.UI.Common
{
    [RequireComponent(typeof(CanvasGroup))]
    public class TopNotificationPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeDuration = 0.3f;

        private Coroutine _notificationCoroutine;

        private void Awake()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }

        public void Show(string message, float duration)
        {
            if (_messageText != null) _messageText.text = message;

            if (_notificationCoroutine != null)
            {
                StopCoroutine(_notificationCoroutine);
            }
            gameObject.SetActive(true);
            _notificationCoroutine = StartCoroutine(ShowAndHideCoroutine(duration));
        }

        private IEnumerator ShowAndHideCoroutine(float visibleDuration)
        {
            // Fade In
            yield return StartCoroutine(Fade(1f));

            // Wait
            yield return new WaitForSeconds(visibleDuration);

            // Fade Out
            yield return StartCoroutine(Fade(0f));

            gameObject.SetActive(false);
            _notificationCoroutine = null;
        }

        private IEnumerator Fade(float targetAlpha)
        {
            float startAlpha = _canvasGroup.alpha;
            float timer = 0f;

            while (timer < _fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / _fadeDuration);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
        }
    }
}
