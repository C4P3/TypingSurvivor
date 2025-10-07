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
        [SerializeField] private TextMeshProUGUI _timeText; // For basic time
        [SerializeField] private TextMeshProUGUI _bestTimeText; // For best time
        [SerializeField] private TextMeshProUGUI _rankText; // For rank
        [SerializeField] private InteractiveButton _rematchButton;
        [SerializeField] private InteractiveButton _mainMenuButton;
        [SerializeField] private InteractiveButton _skipButton; // Full-screen transparent button

        [Header("Panel Canvas Groups")]
        [SerializeField] private CanvasGroup _winLoseBannerCanvasGroup;
        [SerializeField] private CanvasGroup _statsPanelCanvasGroup; // Main container for all stats
        [SerializeField] private CanvasGroup _basicStatsCanvasGroup; // For Time / Best Time
        [SerializeField] private CanvasGroup _newRecordCanvasGroup; // For "New Record!" text
        [SerializeField] private CanvasGroup _rankCanvasGroup; // For Rank info
        [SerializeField] private CanvasGroup _ratingSubPanelCanvasGroup; // For multiplayer rating
        [SerializeField] private CanvasGroup _detailsSubPanelCanvasGroup; // For Blocks, Misses etc.
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

            HideAllCanvasGroups();
        }

        private void OnDestroy()
        {
            _rematchButton.onClick.RemoveAllListeners();
            _mainMenuButton.onClick.RemoveAllListeners();
            _skipButton.onClick.RemoveAllListeners();
        }

        public void Show(GameResultDto resultDto, float personalBest, int playerRank, int totalPlayers)
        {
            if (_sequenceCoroutine != null)
            {
                StopCoroutine(_sequenceCoroutine);
            }

            PrepareUIContent(resultDto, personalBest, playerRank, totalPlayers);
            _sequenceCoroutine = StartCoroutine(ShowSequenceCoroutine(resultDto, personalBest, playerRank, totalPlayers));
            base.Show();
        }

        private void PrepareUIContent(GameResultDto resultDto, float personalBest, int playerRank, int totalPlayers)
        {
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            bool isSinglePlayer = resultDto.FinalPlayerDatas.Length == 1;

            if (isSinglePlayer)
            {
                _resultText.text = "GAME OVER";
                if(_timeText) _timeText.text = $"TIME: {FormatTime(resultDto.FinalGameTime)}";
                if(_bestTimeText) _bestTimeText.text = $"BEST: {FormatTime(personalBest)}";
                if (playerRank > 0 && totalPlayers > 0)
                {
                    float percentile = ((float)playerRank / totalPlayers) * 100f;
                    if(_rankText) _rankText.text = $"RANK: {playerRank} / {totalPlayers} (Top {percentile:F1}%)";
                }
            }
            else
            {
                if (resultDto.IsDraw) _resultText.text = "DRAW";
                else if (resultDto.WinnerClientId == localClientId) _resultText.text = "YOU WIN";
                else _resultText.text = "YOU LOSE";
                if(_timeText) _timeText.text = $"MATCH TIME: {FormatTime(resultDto.FinalGameTime)}";
            }

            StringBuilder detailsBuilder = new StringBuilder();
            detailsBuilder.AppendLine("\n--- STATS ---");
            foreach (var playerData in resultDto.FinalPlayerDatas)
            {
                detailsBuilder.AppendLine($"Player {playerData.ClientId} ({playerData.PlayerName}):");
                detailsBuilder.AppendLine($"  Blocks Destroyed: {playerData.BlocksDestroyed}");
                detailsBuilder.AppendLine($"  Typing Misses: {playerData.TypingMisses}");
            }
            if (_statsText) _statsText.text = detailsBuilder.ToString();

            if (!isSinglePlayer && (resultDto.NewWinnerRating != 0 || resultDto.NewLoserRating != 0))
            {
                // TODO: Populate specific rating text fields
            }
        }

        private IEnumerator ShowSequenceCoroutine(GameResultDto dto, float personalBest, int playerRank, int totalPlayers)
        {
            HideAllCanvasGroups();
            _skipButton.gameObject.SetActive(true);

            yield return FadeCanvasGroup(_winLoseBannerCanvasGroup, true, 0.5f);
            yield return WaitOrSkip(3.0f);
            yield return FadeCanvasGroup(_winLoseBannerCanvasGroup, false, 0.5f);

            yield return FadeCanvasGroup(_statsPanelCanvasGroup, true, 0.5f);
            
            bool isSinglePlayer = dto.FinalPlayerDatas.Length == 1;
            if (isSinglePlayer)
            {
                yield return FadeCanvasGroup(_basicStatsCanvasGroup, true, 0.5f);
                yield return WaitOrSkip(3.0f);

                bool isNewRecord = dto.FinalGameTime > personalBest && personalBest > 0;
                if (isNewRecord)
                {
                    yield return FadeCanvasGroup(_newRecordCanvasGroup, true, 0.5f);
                    yield return WaitOrSkip(2.0f);
                }

                bool hasRank = playerRank > 0 && totalPlayers > 0;
                if (hasRank)
                {
                    yield return FadeCanvasGroup(_rankCanvasGroup, true, 0.5f);
                    yield return WaitOrSkip(3.0f);
                }
            }
            else
            {
                bool isRanked = dto.NewWinnerRating != 0 || dto.NewLoserRating != 0;
                if (isRanked)
                {
                    yield return FadeCanvasGroup(_ratingSubPanelCanvasGroup, true, 0.5f);
                    yield return WaitOrSkip(5.0f);
                }
            }

            yield return FadeCanvasGroup(_detailsSubPanelCanvasGroup, true, 0.5f);
            yield return WaitOrSkip(5.0f);

            yield return FadeCanvasGroup(_actionsPanelCanvasGroup, true, 0.5f);
            _skipButton.gameObject.SetActive(false);
        }

        private void HideAllCanvasGroups()
        {
            if(_winLoseBannerCanvasGroup) _winLoseBannerCanvasGroup.alpha = 0;
            if(_statsPanelCanvasGroup) _statsPanelCanvasGroup.alpha = 0;
            if(_basicStatsCanvasGroup) _basicStatsCanvasGroup.alpha = 0;
            if(_newRecordCanvasGroup) _newRecordCanvasGroup.alpha = 0;
            if(_rankCanvasGroup) _rankCanvasGroup.alpha = 0;
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
