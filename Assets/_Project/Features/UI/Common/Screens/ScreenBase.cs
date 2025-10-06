
using UnityEngine;
using System.Collections;

namespace TypingSurvivor.Features.UI.Common
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class ScreenBase : MonoBehaviour
    {
        [Header("Screen Settings")]
        [SerializeField] protected float _fadeDuration = 0.2f;
        [SerializeField] protected CanvasGroup _canvasGroup;

        protected Coroutine _fadeCoroutine;

        protected virtual void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            // Ensure all screens start in a known, hidden state.
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(Fade(1f, () => {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }));
        }

        public virtual void Hide()
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            _fadeCoroutine = StartCoroutine(Fade(0f, null));
        }

        protected virtual IEnumerator Fade(float targetAlpha, System.Action onCompleted = null)
        {
            // If we are already at the target alpha, complete immediately.
            if (Mathf.Approximately(_canvasGroup.alpha, targetAlpha))
            {
                onCompleted?.Invoke();
                yield break;
            }

            float startAlpha = _canvasGroup.alpha;
            float timer = 0f;

            while (timer < _fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / _fadeDuration);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
            onCompleted?.Invoke();
            _fadeCoroutine = null;
        }
    }
}
