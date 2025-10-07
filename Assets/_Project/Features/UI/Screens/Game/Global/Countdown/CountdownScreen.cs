
using UnityEngine;
using TMPro;
using System.Collections;
using TypingSurvivor.Features.UI.Common;

namespace TypingSurvivor.Features.UI.Screens
{
    public class CountdownScreen : ScreenBase
    {
        [SerializeField] private TextMeshProUGUI _countdownText;

        private Coroutine _countdownCoroutine;

        protected override void Awake()
        {
            base.Awake();
            // Start hidden
            _canvasGroup.alpha = 0;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        public void StartCountdown(System.Action onFinished)
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
            }
            _countdownCoroutine = StartCoroutine(CountdownCoroutine(onFinished));
        }

        private IEnumerator CountdownCoroutine(System.Action onFinished)
        {
            if (_countdownText == null) 
            {
                onFinished?.Invoke();
                yield break;
            }

            _countdownText.text = "3";
            yield return new WaitForSeconds(1f);

            _countdownText.text = "2";
            yield return new WaitForSeconds(1f);

            _countdownText.text = "1";
            yield return new WaitForSeconds(1f);

            _countdownText.text = "GO!";
            yield return new WaitForSeconds(0.5f);

            _countdownCoroutine = null;
            onFinished?.Invoke();
        }
    }
}
