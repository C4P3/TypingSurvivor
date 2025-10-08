using UnityEngine;
using System;
using TypingSurvivor.Features.UI.Common;
using static TypingSurvivor.Features.Game.Gameplay.GameManager;
using TypingSurvivor.Features.UI.Screens.Result;

namespace TypingSurvivor.Features.UI.Screens
{
    /// <summary>
    /// Acts as a factory for the result screen.
    /// It determines which result view prefab to instantiate based on the game outcome.
    /// </summary>
    public class ResultScreen : ScreenBase
    {
        [Header("Single Player Prefabs")]
        [SerializeField] private SinglePlayerResultView _spNormalPrefab;
        [SerializeField] private SinglePlayerResultView _spNewRecordPrefab;

        [Header("Multiplayer Prefabs")]
        [SerializeField] private MultiplayerResultView _mpFreePrefab;
        [SerializeField] private MultiplayerResultView _mpRankedPrefab;

        public event Action OnRematchClicked;
        public event Action OnMainMenuClicked;

        private Component _currentViewInstance;
        private IResultView _currentResultView;
        public IResultView CurrentView => _currentResultView;

        private void OnDestroy()
        {
            if (_currentResultView != null)
            {
                _currentResultView.OnRematchClicked -= OnRematchClicked;
                _currentResultView.OnMainMenuClicked -= OnMainMenuClicked;
            }
        }

        public void Show(GameResultDto resultDto, float personalBest, int playerRank, int totalPlayers)
        {
            base.Show(); // Show the root container

            // Clean up previous instance
            if (_currentViewInstance != null)
            {
                Destroy(_currentViewInstance.gameObject);
            }
            if (_currentResultView != null)
            {
                _currentResultView.OnRematchClicked -= OnRematchClicked;
                _currentResultView.OnMainMenuClicked -= OnMainMenuClicked;
            }

            _currentResultView = InstantiateView(resultDto, personalBest);
            _currentViewInstance = _currentResultView as Component;

            if (_currentResultView != null)
            {
                _currentResultView.OnRematchClicked += OnRematchClicked;
                _currentResultView.OnMainMenuClicked += OnMainMenuClicked;
                _currentResultView.ShowAndPlaySequence(resultDto, personalBest, playerRank, totalPlayers);
            }
        }

        private IResultView InstantiateView(GameResultDto dto, float personalBest)
        {
            bool isSinglePlayer = dto.FinalPlayerDatas.Length == 1;

            if (isSinglePlayer)
            {
                bool isNewRecord = dto.FinalGameTime > personalBest && personalBest > 0;
                return isNewRecord ? Instantiate(_spNewRecordPrefab, transform) : Instantiate(_spNormalPrefab, transform);
            }
            else // Multiplayer
            {
                bool isRanked = dto.NewWinnerRating != 0 || dto.NewLoserRating != 0;
                return isRanked ? Instantiate(_mpRankedPrefab, transform) : Instantiate(_mpFreePrefab, transform);
            }
        }
    }
}
