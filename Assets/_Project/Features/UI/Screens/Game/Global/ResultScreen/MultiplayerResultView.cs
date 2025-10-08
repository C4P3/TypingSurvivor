using System;
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
        [Tooltip("Prefab上で、ローカルプレイヤー（自分）用のカードをアサインしてください")]
        [SerializeField] private PlayerResultCard _localPlayerCard;
        [Tooltip("Prefab上で、リモートプレイヤー（相手）用のカードをアサインしてください")]
        [SerializeField] private PlayerResultCard _remotePlayerCard;

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

            // ローカル・リモートプレイヤーのデータを特定
            PlayerData localPlayerData = dto.FinalPlayerDatas[0].ClientId == Unity.Netcode.NetworkManager.Singleton.LocalClientId ? dto.FinalPlayerDatas[0] : dto.FinalPlayerDatas[1];
            PlayerData remotePlayerData = dto.FinalPlayerDatas[0].ClientId != Unity.Netcode.NetworkManager.Singleton.LocalClientId ? dto.FinalPlayerDatas[0] : dto.FinalPlayerDatas[1];

            // プレイヤーカードにデータを設定
            if (isRanked)
            {
                int localPlayerNewRating = localPlayerData.ClientId == dto.WinnerClientId ? dto.NewWinnerRating : dto.NewLoserRating;
                int remotePlayerNewRating = remotePlayerData.ClientId == dto.WinnerClientId ? dto.NewWinnerRating : dto.NewLoserRating;
                _localPlayerCard.Populate(localPlayerData, true, localPlayerNewRating);
                _remotePlayerCard.Populate(remotePlayerData, true, remotePlayerNewRating);
            }
            else
            {
                _localPlayerCard.Populate(localPlayerData, false, 0);
                _remotePlayerCard.Populate(remotePlayerData, false, 0);
            }
        }
    }
}
