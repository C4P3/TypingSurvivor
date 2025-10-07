using System;
using System.Collections;
using TMPro;
using TypingSurvivor.Features.Game.Gameplay.Data;
using TypingSurvivor.Features.UI.Common;
using UnityEngine;
using static TypingSurvivor.Features.Game.Gameplay.GameManager;

namespace TypingSurvivor.Features.UI.Screens.Result
{
    public class RankedMatchResultView : ScreenBase, IResultView
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _winLoseText;
        [SerializeField] private PlayerResultCard _player1Card;
        [SerializeField] private PlayerResultCard _player2Card;
        [SerializeField] private InteractiveButton _skipButton;
        [SerializeField] private InteractiveButton _rematchButton;
        [SerializeField] private InteractiveButton _mainMenuButton;

        [Header("Animatable Panels")]
        [SerializeField] private CanvasGroup _winLoseBannerCanvasGroup;
        [SerializeField] private CanvasGroup _playerCardsCanvasGroup;
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

        public void ShowAndPlaySequence(GameResultDto dto, float personalBest, int playerRank, int totalPlayers)
        {
            PrepareUIContent(dto);
            base.Show(); // Fade in the root panel
            StartCoroutine(PlaySequence());
        }

        private void PrepareUIContent(GameResultDto dto)
        {
            bool localPlayerWon = dto.WinnerClientId == Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            if (dto.IsDraw) _winLoseText.text = "DRAW";
            else _winLoseText.text = localPlayerWon ? "YOU WIN" : "GAME OVER";

            // TODO: A more robust way to determine winner/loser data
            PlayerData winnerData = dto.FinalPlayerDatas[0].ClientId == dto.WinnerClientId ? dto.FinalPlayerDatas[0] : dto.FinalPlayerDatas[1];

            // Assuming player 1 card is for the local player
            var localPlayerCard = dto.FinalPlayerDatas[0].ClientId == Unity.Netcode.NetworkManager.Singleton.LocalClientId ? _player1Card : _player2Card;
            var remotePlayerCard = dto.FinalPlayerDatas[0].ClientId != Unity.Netcode.NetworkManager.Singleton.LocalClientId ? _player1Card : _player2Card;

            var localPlayerData = dto.FinalPlayerDatas[0].ClientId == Unity.Netcode.NetworkManager.Singleton.LocalClientId ? dto.FinalPlayerDatas[0] : dto.FinalPlayerDatas[1];
            var remotePlayerData = dto.FinalPlayerDatas[0].ClientId != Unity.Netcode.NetworkManager.Singleton.LocalClientId ? dto.FinalPlayerDatas[0] : dto.FinalPlayerDatas[1];

            int localPlayerNewRating = localPlayerData.ClientId == winnerData.ClientId ? dto.NewWinnerRating : dto.NewLoserRating;
            int remotePlayerNewRating = remotePlayerData.ClientId == winnerData.ClientId ? dto.NewWinnerRating : dto.NewLoserRating;

            localPlayerCard.Populate(localPlayerData, true, localPlayerNewRating);
            remotePlayerCard.Populate(remotePlayerData, true, remotePlayerNewRating);
        }

        public IEnumerator PlaySequence()
        {
            _winLoseBannerCanvasGroup.alpha = 0;
            _playerCardsCanvasGroup.alpha = 0;
            _actionsCanvasGroup.alpha = 0;
            if(_skipButton) _skipButton.gameObject.SetActive(true);

            // Step 1: Result Banner
            yield return StartCoroutine(FadeCanvasGroup(_winLoseBannerCanvasGroup, true, 0.5f));
            yield return StartCoroutine(WaitOrSkip(1.5f));

            // Step 2: Player Info
            yield return StartCoroutine(FadeCanvasGroup(_winLoseBannerCanvasGroup, false, 0.3f));
            yield return StartCoroutine(FadeCanvasGroup(_playerCardsCanvasGroup, true, 0.5f));
            yield return StartCoroutine(WaitOrSkip(2.5f));

            // Step 3: Action Buttons
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
    }
}