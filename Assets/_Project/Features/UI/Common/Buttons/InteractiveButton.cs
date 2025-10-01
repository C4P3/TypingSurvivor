
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using TypingSurvivor.Features.Core.Audio;

namespace TypingSurvivor.Features.UI.Common
{
    [RequireComponent(typeof(RectTransform))]
    public class InteractiveButton : Button, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Sound Effects")]
        [SerializeField] private SoundId _hoverSound = SoundId.UIButtonHover;
        [SerializeField] private SoundId _clickSound = SoundId.UIButtonClick;

        [Header("Animation Settings")]
        [SerializeField] private float _hoverScale = 1.05f;
        [SerializeField] private float _clickScale = 0.95f;
        [SerializeField] private float _transitionDuration = 0.1f;

        private Vector3 _originalScale;
        private Coroutine _animationCoroutine;

        protected override void Awake()
        {
            base.Awake();
            _originalScale = transform.localScale;
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            if (!IsInteractable()) return;

            SfxManager.Instance?.PlaySfx(_hoverSound);
            AnimateScale(_originalScale * _hoverScale);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            if (!IsInteractable()) return;

            AnimateScale(_originalScale);
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (!IsInteractable()) return;

            AnimateScale(_originalScale * _clickScale);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            if (!IsInteractable()) return;

            SfxManager.Instance?.PlaySfx(_clickSound);
            AnimateScale(_originalScale * _hoverScale);
        }

        private void AnimateScale(Vector3 targetScale)
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }
            _animationCoroutine = StartCoroutine(ScaleCoroutine(targetScale));
        }

        private IEnumerator ScaleCoroutine(Vector3 targetScale)
        {
            Vector3 currentScale = transform.localScale;
            float timer = 0f;

            while (timer < _transitionDuration)
            {
                timer += Time.unscaledDeltaTime; // Use unscaled time for UI animations
                transform.localScale = Vector3.Lerp(currentScale, targetScale, timer / _transitionDuration);
                yield return null;
            }

            transform.localScale = targetScale;
            _animationCoroutine = null;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            // Ensure scale is reset if the button is disabled mid-animation
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
            transform.localScale = _originalScale;
        }
    }
}
