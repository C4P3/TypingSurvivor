
using UnityEngine;
using System.Collections;

namespace TypingSurvivor.Features.UI.Common
{
    /// <summary>
    /// Base class for all UI screens/panels that can be shown or hidden with a fade effect.
    /// Requires a CanvasGroup component on the same GameObject.
    /// </summary>
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
        }

        /// <summary>
        /// Shows the screen with a fade-in effect.
        /// </summary>
        public virtual void Show()
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(Fade(1f));
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        /// <summary>
        /// Hides the screen with a fade-out effect.
        /// </summary>
        public virtual void Hide()
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(Fade(0f));
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        protected virtual IEnumerator Fade(float targetAlpha)
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
            _fadeCoroutine = null;
        }
    }
}
