using System;
using System.Text;
using TMPro;
using TypingSurvivor.Features.UI.Common;
using UnityEngine;
using static TypingSurvivor.Features.Game.Gameplay.GameManager;

namespace TypingSurvivor.Features.UI.Screens.Result
{
    [RequireComponent(typeof(AnimationSequencer))]
    public class SinglePlayerResultView : MonoBehaviour, IResultView
    {
        public event Action OnRematchClicked;
        public event Action OnMainMenuClicked;

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private TextMeshProUGUI _bestTimeText;
        [SerializeField] private TextMeshProUGUI _rankText;
        [SerializeField] private TextMeshProUGUI _statsText; // For WPM, Blocks, Misses

        [Header("Buttons")]
        [SerializeField] private InteractiveButton _rematchButton;
        [SerializeField] private InteractiveButton _mainMenuButton;

        private AnimationSequencer[] _allSequencersInHierarchy;

        private void Awake()
        {
            _allSequencersInHierarchy = GetComponentsInChildren<AnimationSequencer>(true);
            _rematchButton?.onClick.AddListener(() => OnRematchClicked?.Invoke());
            _mainMenuButton?.onClick.AddListener(() => OnMainMenuClicked?.Invoke());
        }

        public void ShowAndPlaySequence(GameResultDto dto, float personalBest, int playerRank, int totalPlayers)
        {
            PrepareUIContent(dto, personalBest, playerRank, totalPlayers);
            
            // Play the root sequencer
            var rootSequencer = GetComponent<AnimationSequencer>();
            if (rootSequencer != null)
            {
                rootSequencer.Play();
            }
        }

        private void SetStepEnabledInAllSequencers(string stepName, bool isEnabled)
        {
            foreach (var sequencer in _allSequencersInHierarchy)
            {
                sequencer.SetStepEnabled(stepName, isEnabled);
            }
        }

        private void PrepareUIContent(GameResultDto dto, float personalBest, int playerRank, int totalPlayers)
        {
            var playerData = dto.FinalPlayerDatas[0];
            bool isNewRecord = dto.FinalGameTime > personalBest && personalBest > 0;

            SetStepEnabledInAllSequencers("ShowNewRecord", isNewRecord);

            // データをUIに設定
            _timeText.text = $"TIME: {FormatTime(dto.FinalGameTime)}";
            _bestTimeText.text = $"BEST: {FormatTime(isNewRecord ? dto.FinalGameTime : personalBest)}";

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

        private string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60F);
            int seconds = Mathf.FloorToInt(timeInSeconds - minutes * 60);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}
