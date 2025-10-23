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

        private bool _lastWasDraw;

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

        public void UpdateRatingInfo(RatingsDto ratingsDto)
        {
            // This assumes the same sorting logic as PrepareUIContent
            var sortedPlayers = GetSortedPlayersFromLastDto();
            if (sortedPlayers == null || sortedPlayers.Count < 2) return;

            var player1Data = sortedPlayers[0];
            var player2Data = sortedPlayers[1];

            int player1NewRating, player1OldRating, player2NewRating, player2OldRating;

            if (_lastWasDraw)
            {
                // In a draw, the DTO sends player1's ratings in the "winner" slots
                // and player2's ratings in the "loser" slots, sorted by ClientId on the server.
                player1OldRating = ratingsDto.OldWinnerRating;
                player1NewRating = ratingsDto.NewWinnerRating;
                player2OldRating = ratingsDto.OldLoserRating;
                player2NewRating = ratingsDto.NewLoserRating;
            }
            else
            {
                player1NewRating = player1Data.ClientId == _lastWinnerId ? ratingsDto.NewWinnerRating : ratingsDto.NewLoserRating;
                player1OldRating = player1Data.ClientId == _lastWinnerId ? ratingsDto.OldWinnerRating : ratingsDto.OldLoserRating;

                player2NewRating = player2Data.ClientId == _lastWinnerId ? ratingsDto.NewWinnerRating : ratingsDto.NewLoserRating;
                player2OldRating = player2Data.ClientId == _lastWinnerId ? ratingsDto.OldWinnerRating : ratingsDto.OldLoserRating;
            }

            if (_player1Card) _player1Card.UpdateRating(player1NewRating, player1OldRating);
            if (_player2Card) _player2Card.UpdateRating(player2NewRating, player2OldRating);
        }

        private void SetStepEnabledInAllSequencers(string stepName, bool isEnabled)
        {
            foreach (var sequencer in _allSequencersInHierarchy)
            {
                sequencer.SetStepEnabled(stepName, isEnabled);
            }
        }

        // Cache the DTO to re-use its data for rating updates
        private PlayerData[] _lastFinalPlayerDatas;
        private ulong _lastWinnerId;

        private System.Collections.Generic.List<PlayerData> GetSortedPlayersFromLastDto()
        {
            if (_lastFinalPlayerDatas == null) return null;
            return _lastFinalPlayerDatas.OrderBy(p => p.ClientId).ToList();
        }

        private void PrepareUIContent(GameResultDto dto)
        {
            // Cache data for later use in UpdateRatingInfo
            _lastFinalPlayerDatas = dto.FinalPlayerDatas;
            _lastWinnerId = dto.WinnerClientId;
            _lastWasDraw = dto.IsDraw;

            // Reset state for new results
            _opponentDisconnected = false;
            _showRematchRequesterUntil = -1f;
            _rematchButton.interactable = true;

            // If the game ended because of a disconnection, show the specific message.
            if (dto.OpponentDisconnected)
            {
                NotifyOpponentDisconnected();
            }

            // For ranked matches, the rating section will be shown, but with "Calculating..."
            bool isRanked = Core.App.AppManager.Instance.GameMode == Core.App.GameModeType.RankedMatch;

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
            var sortedPlayers = GetSortedPlayersFromLastDto();
            if (sortedPlayers == null || sortedPlayers.Count < 2) return; // Should not happen in multiplayer

            var player1Data = sortedPlayers[0];
            var player2Data = sortedPlayers[1];

            // プレイヤーカードにデータを設定 (レート情報は含めない)
            if(_player1Card) _player1Card.Populate(player1Data, isRanked);
            if(_player2Card) _player2Card.Populate(player2Data, isRanked);
        }
    }
}
