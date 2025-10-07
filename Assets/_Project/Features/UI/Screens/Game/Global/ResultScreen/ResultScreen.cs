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
        [Header("Root Layout Panels")]
        [SerializeField] private GameObject _singlePlayerRoot;
        [SerializeField] private GameObject _multiPlayerRoot;

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
        public CanvasGroup _winLoseBannerCanvasGroup;
        public CanvasGroup _statsPanelCanvasGroup; // Main container for all stats
        public CanvasGroup _basicStatsCanvasGroup; // For Time / Best Time
        public CanvasGroup _newRecordCanvasGroup; // For "New Record!" text
        public CanvasGroup _rankCanvasGroup; // For Rank info
        public CanvasGroup _ratingSubPanelCanvasGroup; // For multiplayer rating
        public CanvasGroup _detailsSubPanelCanvasGroup; // For Blocks, Misses etc.
        public CanvasGroup _actionsPanelCanvasGroup;

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

            // Activate the correct root panel based on player count
            bool isSinglePlayer = resultDto.FinalPlayerDatas.Length == 1;
            _singlePlayerRoot?.SetActive(isSinglePlayer);
            _multiPlayerRoot?.SetActive(!isSinglePlayer);

            PrepareUIContent(resultDto, personalBest, playerRank, totalPlayers);

            IResultSequenceStrategy strategy = GetStrategyForCurrentMode(resultDto);
            _sequenceCoroutine = StartCoroutine(strategy.ExecuteSequence(this, resultDto, personalBest, playerRank, totalPlayers));
            
            base.Show();
        }

        private IResultSequenceStrategy GetStrategyForCurrentMode(GameResultDto resultDto)
        {
            var gameMode = Core.App.AppManager.Instance.GameMode;
            bool isRanked = resultDto.NewWinnerRating != 0 || resultDto.NewLoserRating != 0;

            if (gameMode == Core.App.GameModeType.SinglePlayer)
            {
                return new SinglePlayerResultSequence();
            }
            if (isRanked)
            {
                return new RankedMatchResultSequence();
            }
            return new FreeMatchResultSequence();
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

                // Calculate WPM
                float wpm = 0;
                if (playerData.TotalTimeTyping > 0)
                {
                    wpm = (playerData.TotalCharsTyped / 5.0f) / (playerData.TotalTimeTyping / 60.0f);
                }

                // Calculate Miss Rate
                float missRate = 0;
                if (playerData.TotalKeyPresses > 0)
                {
                    missRate = (float)playerData.TypingMisses / playerData.TotalKeyPresses * 100.0f;
                }

                detailsBuilder.AppendLine($"  WPM: {wpm:F1}");
                detailsBuilder.AppendLine($"  Blocks Destroyed: {playerData.BlocksDestroyed}");
                detailsBuilder.AppendLine($"  Typing Misses: {playerData.TypingMisses} ({missRate:F1}%)");
            }
            if (_statsText) _statsText.text = detailsBuilder.ToString();

            if (!isSinglePlayer && (resultDto.NewWinnerRating != 0 || resultDto.NewLoserRating != 0))
            {
                // TODO: Populate specific rating text fields
            }
        }

        public void HideAllCanvasGroups()
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

        public void SetSkipButtonActive(bool isActive)
        {
            if(_skipButton) _skipButton.gameObject.SetActive(isActive);
        }

        public IEnumerator WaitOrSkip(float duration)
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

        public IEnumerator FadeCanvasGroup(CanvasGroup cg, bool fadeIn, float duration)
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
