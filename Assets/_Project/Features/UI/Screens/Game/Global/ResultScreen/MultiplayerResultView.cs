using System;
using System.Linq;
using TMPro;
using TypingSurvivor.Features.Game.Gameplay.Data;
using TypingSurvivor.Features.UI.Common;
using UnityEngine;
using static TypingSurvivor.Features.Game.Gameplay.GameManager;

namespace TypingSurvivor.Features.UI.Screens.Result
{
    [RequireComponent(typeof(AnimationSequencer))]
    public class MultiplayerResultView : MonoBehaviour, IResultView
    {
        public event Action OnRematchClicked;
        public event Action OnMainMenuClicked;

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _winLoseDrawText;
        [Tooltip("Player 1 (ClientIdが小さい方) のカード")]
        [SerializeField] private PlayerResultCard _player1Card;
        [Tooltip("Player 2 (ClientIdが大きい方) のカード")]
        [SerializeField] private PlayerResultCard _player2Card;
        [Tooltip("再戦タイマー")]
        [SerializeField] private TextMeshProUGUI _rematchTimerText;
        [Tooltip("切断メッセージ")]
        [SerializeField] private TextMeshProUGUI _disconnectMessageText;

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
            PrepareUIContent(dto);
            
            var rootSequencer = GetComponent<AnimationSequencer>();
            if (rootSequencer != null)
            {
                rootSequencer.Play();
            }
        }

        public void UpdateRematchTimer(float remainingTime)
        {
            if (_rematchTimerText == null) return;

            if (remainingTime > 0)
            {
                // 残り時間を表示
                _rematchTimerText.text = $"再戦待機中... あと {Mathf.CeilToInt(remainingTime)} 秒";
            }
            else
            {
                // 時間切れ
                _rematchTimerText.text = "再戦時間が終了しました";
                _rematchButton.interactable = false; // 再戦ボタンを無効化
            }
        }

        public void NotifyOpponentDisconnected()
        {
            if (_disconnectMessageText)
            {
                _disconnectMessageText.text = "対戦相手が退出しました";
                _disconnectMessageText.gameObject.SetActive(true);
            }
            if (_rematchTimerText)
            {
                 _rematchTimerText.gameObject.SetActive(false); // タイマーを非表示に
            }
            _rematchButton.interactable = false; // 再戦ボタンを無効化
        }

        private void SetStepEnabledInAllSequencers(string stepName, bool isEnabled)
        {
            foreach (var sequencer in _allSequencersInHierarchy)
            {
                sequencer.SetStepEnabled(stepName, isEnabled);
            }
        }

        private void PrepareUIContent(GameResultDto dto)
        {
            // ランクマッチかどうかを判定
            bool isRanked = dto.NewWinnerRating != 0 || dto.NewLoserRating != 0;

            // 例えば、ランク戦の時だけ特別な演出ステップを有効にする場合
            // SetStepEnabledInAllSequencers("ShowRankAnimation", isRanked);

            // 勝敗テキストを設定
            bool localPlayerWon = dto.WinnerClientId == Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            if (dto.IsDraw)
            {
                _winLoseDrawText.text = "DRAW";
            }
            else
            {
                _winLoseDrawText.text = localPlayerWon ? "YOU WIN" : "YOU LOSE";
            }

            // CameraManagerと同様に、ClientIdでプレイヤーをソートし、P1/P2を確定させる
            var sortedPlayers = dto.FinalPlayerDatas.OrderBy(p => p.ClientId).ToList();
            if (sortedPlayers.Count < 2) return; // Should not happen in multiplayer

            var player1Data = sortedPlayers[0];
            var player2Data = sortedPlayers[1];

            // プレイヤーカードにデータを設定
            if (isRanked)
            {
                int player1NewRating = player1Data.ClientId == dto.WinnerClientId ? dto.NewWinnerRating : dto.NewLoserRating;
                int player2NewRating = player2Data.ClientId == dto.WinnerClientId ? dto.NewWinnerRating : dto.NewLoserRating;
                if(_player1Card) _player1Card.Populate(player1Data, true, player1NewRating);
                if(_player2Card) _player2Card.Populate(player2Data, true, player2NewRating);
            }
            else
            {
                if(_player1Card) _player1Card.Populate(player1Data, false, 0);
                if(_player2Card) _player2Card.Populate(player2Data, false, 0);
            }
        }
    }
}
