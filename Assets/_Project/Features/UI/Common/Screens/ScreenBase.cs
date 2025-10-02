
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
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            gameObject.SetActive(true);
            _fadeCoroutine = StartCoroutine(Fade(1f, () => {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }));
        }

        public virtual void Hide()
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            
            // Immediately disable interaction
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            _fadeCoroutine = StartCoroutine(Fade(0f, () => {
                gameObject.SetActive(false);
            }));
        }

        protected virtual IEnumerator Fade(float targetAlpha, System.Action onCompleted = null)
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
            onCompleted?.Invoke();
            _fadeCoroutine = null;
        }
    }
}
