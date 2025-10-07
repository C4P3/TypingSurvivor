using UnityEngine;
using TMPro;
using System;
using TypingSurvivor.Features.UI.Common;
using static TypingSurvivor.Features.Game.Gameplay.GameManager;
using Unity.Netcode;
using System.Text;
using System.Collections;

namespace TypingSurvivor.Features.UI.Screens
{
    public class ResultScreen : ScreenBase
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _resultText; // e.g., "YOU WIN"
        [SerializeField] private TextMeshProUGUI _statsText; // For detailed stats
        [SerializeField] private InteractiveButton _rematchButton;
        [SerializeField] private InteractiveButton _mainMenuButton;
        [SerializeField] private InteractiveButton _skipButton; // Full-screen transparent button

        [Header("Panel Canvas Groups")]
        [SerializeField] private CanvasGroup _winLoseBannerCanvasGroup;
        [SerializeField] private CanvasGroup _statsPanelCanvasGroup;
        [SerializeField] private CanvasGroup _ratingSubPanelCanvasGroup;
        [SerializeField] private CanvasGroup _detailsSubPanelCanvasGroup;
        [SerializeField] private CanvasGroup _actionsPanelCanvasGroup;

        public event Action OnRematchClicked;
        public event Action OnMainMenuClicked;

        private bool _skipWait = false;
        private Coroutine _sequenceCoroutine;

        protected override void Awake()
        {
            base.Awake();
            _rematchButton.onClick.AddListener(() => OnRematchClicked?.Invoke());
            _mainMenuButton.onClick.AddListener(() => OnMainMenuClicked?.Invoke());
            _skipButton.onClick.AddListener(() => _skipWait = true);

            // Start with all groups hidden
            HideAllCanvasGroups();
        }

        private void OnDestroy()
        {
            _rematchButton.onClick.RemoveAllListeners();
            _mainMenuButton.onClick.RemoveAllListeners();
            _skipButton.onClick.RemoveAllListeners();
        }

        public void Show(GameResultDto resultDto)
        {
            if (_sequenceCoroutine != null)
            {
                StopCoroutine(_sequenceCoroutine);
            }

            // 1. Prepare all UI content before starting the animation
            PrepareUIContent(resultDto);

            // 2. Start the display sequence
            _sequenceCoroutine = StartCoroutine(ShowSequenceCoroutine(resultDto));
            
            base.Show(); // Fade in the root canvas group
        }

        private void PrepareUIContent(GameResultDto resultDto)
        {
            // Determine Win/Loss/Draw for the local player
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            if (resultDto.IsDraw)
            {
                _resultText.text = "DRAW";
            }
            else if (resultDto.WinnerClientId == localClientId)
            {
                _resultText.text = "YOU WIN";
            }
            else
            {
                _resultText.text = "YOU LOSE";
            }

            // Build the detailed stats string
            StringBuilder statsBuilder = new StringBuilder();
            foreach (var playerData in resultDto.FinalPlayerDatas)
            {
                statsBuilder.AppendLine($"--- Player {playerData.ClientId} ---");
                statsBuilder.AppendLine($"Score: {playerData.Score}");
                statsBuilder.AppendLine($"Blocks Destroyed: {playerData.BlocksDestroyed}");
                statsBuilder.AppendLine($"Typing Misses: {playerData.TypingMisses}");
                statsBuilder.AppendLine();
            }
            _statsText.text = statsBuilder.ToString();

            // TODO: Populate rating change text fields here
        }

        private IEnumerator ShowSequenceCoroutine(GameResultDto dto)
        {
            HideAllCanvasGroups();
            _skipButton.gameObject.SetActive(true);

            // Step 1: Show Win/Loss Banner
            yield return FadeCanvasGroup(_winLoseBannerCanvasGroup, true, 0.5f);
            yield return WaitOrSkip(5.0f);
            yield return FadeCanvasGroup(_winLoseBannerCanvasGroup, false, 0.5f);

            // Step 2: Show Stats Panel & Rating (if applicable)
            yield return FadeCanvasGroup(_statsPanelCanvasGroup, true, 0.5f);
            bool isRanked = dto.NewWinnerRating != 0 || dto.NewLoserRating != 0;
            if (isRanked)
            {
                yield return FadeCanvasGroup(_ratingSubPanelCanvasGroup, true, 0.5f);
                yield return WaitOrSkip(5.0f);
            }

            // Step 3: Show Detailed Stats
            yield return FadeCanvasGroup(_detailsSubPanelCanvasGroup, true, 0.5f);
            yield return WaitOrSkip(5.0f);

            // Step 4: Show Action Buttons
            yield return FadeCanvasGroup(_actionsPanelCanvasGroup, true, 0.5f);
            _skipButton.gameObject.SetActive(false);
        }

        private void HideAllCanvasGroups()
        {
            if(_winLoseBannerCanvasGroup) _winLoseBannerCanvasGroup.alpha = 0;
            if(_statsPanelCanvasGroup) _statsPanelCanvasGroup.alpha = 0;
            if(_ratingSubPanelCanvasGroup) _ratingSubPanelCanvasGroup.alpha = 0;
            if(_detailsSubPanelCanvasGroup) _detailsSubPanelCanvasGroup.alpha = 0;
            if(_actionsPanelCanvasGroup) _actionsPanelCanvasGroup.alpha = 0;
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
            _skipWait = false; // Reset for the next step
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
    }
}
