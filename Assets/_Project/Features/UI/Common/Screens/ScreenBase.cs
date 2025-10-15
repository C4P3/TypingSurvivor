
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

        public bool IsVisible => _canvasGroup != null && _canvasGroup.alpha > 0;

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
            // 実行中のフェードがあれば止める
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

            // 子階層のAnimationSequencerをすべてリセットする
            var sequencers = GetComponentsInChildren<AnimationSequencer>(true);
            foreach (var sequencer in sequencers)
            {
                if (sequencer != null)
                {
                    sequencer.ResetSequenceAndStop();
                }
            }

            // "現在表示されている" 子階層の ScreenBase をすべて非表示にする
            var childScreens = GetComponentsInChildren<ScreenBase>(true);
            foreach (var screen in childScreens)
            {
                if (screen != null && screen != this && screen.IsVisible)
                {
                    screen.Hide();
                }
            }

            // 自身のインタラクションを無効にし、フェードアウトを開始する
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            
            if (gameObject.activeInHierarchy)
            {
                _fadeCoroutine = StartCoroutine(Fade(0f, null));
            }
            else
            {
                // ゲームオブジェクトが非アクティブなら、即座にalphaを0にする
                if (_canvasGroup != null) _canvasGroup.alpha = 0;
            }
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
