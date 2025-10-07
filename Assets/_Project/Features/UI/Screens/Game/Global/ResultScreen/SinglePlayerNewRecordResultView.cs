using System;
using System.Collections;
using System.Text;
using TMPro;
using TypingSurvivor.Features.UI.Common;
using UnityEngine;
using static TypingSurvivor.Features.Game.Gameplay.GameManager;

namespace TypingSurvivor.Features.UI.Screens.Result
{
    public class SinglePlayerNewRecordResultView : ScreenBase, IResultView
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _winLoseText; // For "New Record!"
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private TextMeshProUGUI _bestTimeText;
        [SerializeField] private TextMeshProUGUI _rankText;
        [SerializeField] private TextMeshProUGUI _statsText; // For WPM, Blocks, Misses
        [SerializeField] private InteractiveButton _skipButton;
        [SerializeField] private InteractiveButton _rematchButton;
        [SerializeField] private InteractiveButton _mainMenuButton;

        [Header("Sub-Panel Canvas Groups")]
        [SerializeField] private CanvasGroup _winLoseBannerCanvasGroup;
        [SerializeField] private CanvasGroup _mainContentCanvasGroup;
        [SerializeField] private CanvasGroup _detailsCanvasGroup;
        [SerializeField] private CanvasGroup _actionsCanvasGroup;

        public event Action OnRematchClicked;
        public event Action OnMainMenuClicked;

        private bool _skipWait = false;

        protected override void Awake()
        {
            base.Awake();
            _skipButton?.onClick.AddListener(() => _skipWait = true);
            _rematchButton?.onClick.AddListener(() => OnRematchClicked?.Invoke());
            _mainMenuButton?.onClick.AddListener(() => OnMainMenuClicked?.Invoke());
        }

        public void Populate(GameResultDto dto, float personalBest, int playerRank, int totalPlayers)
        {
            var playerData = dto.FinalPlayerDatas[0];
            _winLoseText.text = "New Record!";

            _timeText.text = $"TIME: {FormatTime(dto.FinalGameTime)}";
            _bestTimeText.text = $"BEST: {FormatTime(dto.FinalGameTime)}"; // The new record is the best time

            if (playerRank > 0 && totalPlayers > 0)
            {
                float percentile = ((float)playerRank / totalPlayers) * 100f;
                _rankText.text = $"RANK: {playerRank} / {totalPlayers} (Top {percentile:F1}%)";
            }
            else
            {
                _rankText.text = "RANK: Unranked";
            }

            float wpm = 0;
            if (playerData.TotalTimeTyping > 0) wpm = (playerData.TotalCharsTyped / 5.0f) / (playerData.TotalTimeTyping / 60.0f);
            
            float missRate = 0;
            if (playerData.TotalKeyPresses > 0) missRate = (float)playerData.TypingMisses / (float)playerData.TotalKeyPresses * 100.0f;

            var statsBuilder = new StringBuilder();
            statsBuilder.AppendLine($"WPM: {wpm:F1}");
            statsBuilder.AppendLine($"Blocks: {playerData.BlocksDestroyed}");
            statsBuilder.AppendLine($"Miss: {playerData.TypingMisses} ({missRate:F1}%)");
            _statsText.text = statsBuilder.ToString();
        }

        public IEnumerator PlaySequence()
        {
            _winLoseBannerCanvasGroup.alpha = 0;
            _mainContentCanvasGroup.alpha = 0;
            _detailsCanvasGroup.alpha = 0;
            _actionsCanvasGroup.alpha = 0;
            if(_skipButton) _skipButton.gameObject.SetActive(true);

            // Step 1: Result Banner
            yield return StartCoroutine(FadeCanvasGroup(_winLoseBannerCanvasGroup, true, 0.5f));
            yield return StartCoroutine(WaitOrSkip(1.5f));

            // Step 2: Main Results
            yield return StartCoroutine(FadeCanvasGroup(_winLoseBannerCanvasGroup, false, 0.3f));
            yield return StartCoroutine(FadeCanvasGroup(_mainContentCanvasGroup, true, 0.5f));
            yield return StartCoroutine(WaitOrSkip(2.0f));

            // Step 3: Detailed Info
            yield return StartCoroutine(FadeCanvasGroup(_detailsCanvasGroup, true, 0.5f));
            yield return StartCoroutine(WaitOrSkip(2.0f));

            // Step 4: Action Buttons
            if(_skipButton) _skipButton.gameObject.SetActive(false);
            yield return StartCoroutine(FadeCanvasGroup(_actionsCanvasGroup, true, 0.5f));
        }

        private IEnumerator WaitOrSkip(float duration)
        {
            _skipWait = false;
            float timer = 0;
            while (timer < duration && !_skipWait)
            {
                timer += Time.deltaTime;
                yield return null;
            }
            _skipWait = false;
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, bool fadeIn, float duration)
        {
            if (cg == null) yield break;
            float startAlpha = fadeIn ? 0f : 1f;
            float endAlpha = fadeIn ? 1f : 0f;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
                yield return null;
            }
            cg.alpha = endAlpha;
        }

        private string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60F);
            int seconds = Mathf.FloorToInt(timeInSeconds - minutes * 60);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}