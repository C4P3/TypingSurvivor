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
        [Tooltip("共有テキストエリア (タイマー、希望者数、切断通知)")]
        [SerializeField] private TextMeshProUGUI _sharedStatusText;

        [Header("Buttons")]
        [SerializeField] private InteractiveButton _rematchButton;
        [SerializeField] private InteractiveButton _mainMenuButton;

        private AnimationSequencer[] _allSequencersInHierarchy;

        // State for priority display
        private bool _opponentDisconnected = false;
        private float _showRematchRequesterUntil = -1f;
        private int _requesterCount = 0;
        private int _totalPlayers = 0;

        private void Awake()
        {
            _allSequencersInHierarchy = GetComponentsInChildren<AnimationSequencer>(true);
            _rematchButton?.onClick.AddListener(() => OnRematchClicked?.Invoke());
            _mainMenuButton?.onClick.AddListener(() => OnMainMenuClicked?.Invoke());
            if (_sharedStatusText) _sharedStatusText.text = ""; // Clear text initially
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
            if (_sharedStatusText == null) return;

            // 1. Highest priority: Opponent disconnected
            if (_opponentDisconnected)
            {
                return; // Message is already set, do nothing
            }

            // 2. Medium priority: Show rematch requester count for 10 seconds
            if (Time.time < _showRematchRequesterUntil)
            {
                _sharedStatusText.text = $"再戦希望者 {_requesterCount} / {_totalPlayers}";
            }
            // 3. Lowest priority: Show auto-exit timer
            else
            {
                if (remainingTime > 0)
                {
                    _sharedStatusText.text = $"自動退出まで残り {Mathf.CeilToInt(remainingTime)} 秒";
                }
                else
                {
                    _sharedStatusText.text = ""; // Timer expired, will be kicked soon
                }
            }
        }

        public void UpdateRematchRequesterCount(int count, int total)
        {
            _requesterCount = count;
            _totalPlayers = total;
            _showRematchRequesterUntil = Time.time + 10f;
        }

        public void NotifyOpponentDisconnected()
        {
            _opponentDisconnected = true;
            if (_sharedStatusText)
            {
                _sharedStatusText.text = "対戦相手が退出しました";
            }
            _rematchButton.interactable = false; // Disable rematch button
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
            // Reset state for new results
            _opponentDisconnected = false;
            _showRematchRequesterUntil = -1f;
            _rematchButton.interactable = true;

            // ランクマッチかどうかを判定
            bool isRanked = dto.NewWinnerRating != 0 || dto.NewLoserRating != 0;

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
