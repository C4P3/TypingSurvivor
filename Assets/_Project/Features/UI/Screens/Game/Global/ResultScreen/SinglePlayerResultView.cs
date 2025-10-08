using System;
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
        [Tooltip("WPM, Blocks, Missesなどの統計情報を表示するカード")]
        [SerializeField] private PlayerResultCard _playerCard;

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

            // "ShowNewRecord" という名前のステップがシーケンサーにあれば、条件に応じて有効/無効を切り替える
            SetStepEnabledInAllSequencers("ShowNewRecord", isNewRecord);

            // タイムやランクなど、この画面固有のUIを更新
            if(_timeText) _timeText.text = $"TIME: {FormatTime(dto.FinalGameTime)}";
            if(_bestTimeText) _bestTimeText.text = $"BEST: {FormatTime(isNewRecord ? dto.FinalGameTime : personalBest)}";

            if (_rankText != null)
            {
                if (playerRank > 0 && totalPlayers > 0)
                {
                    float percentile = ((float)playerRank / totalPlayers) * 100f;
                    _rankText.text = $"RANK: {playerRank} / {totalPlayers} (Top {percentile:F1}%)";
                }
                else
                {
                    _rankText.text = "RANK: Unranked";
                }
            }

            // PlayerResultCardに統計データの設定を委譲
            if (_playerCard != null)
            {
                // シングルプレイなので、isRankedはfalse, newRatingは0
                _playerCard.Populate(playerData, false, 0);
            }
        }

        private string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60F);
            int seconds = Mathf.FloorToInt(timeInSeconds - minutes * 60);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}
