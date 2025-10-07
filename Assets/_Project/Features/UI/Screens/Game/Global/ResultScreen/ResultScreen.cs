using UnityEngine;
using System;
using TypingSurvivor.Features.UI.Common;
using static TypingSurvivor.Features.Game.Gameplay.GameManager;
using TypingSurvivor.Features.UI.Screens.Result;

namespace TypingSurvivor.Features.UI.Screens
{
    /// <summary>
    /// Acts as a factory and conductor for the result screen.
    /// It determines which result view prefab to instantiate based on the game outcome.
    /// </summary>
    public class ResultScreen : ScreenBase
    {
        [Header("Result View Prefabs")]
        [SerializeField] private SinglePlayerNormalResultView _spNormalPrefab;
        [SerializeField] private SinglePlayerNewRecordResultView _spNewRecordPrefab;
        [SerializeField] private RankedMatchResultView _rankedPrefab;
        [SerializeField] private FreeMatchResultView _freePrefab;

        public event Action OnRematchClicked;
        public event Action OnMainMenuClicked;

        private ScreenBase _currentViewInstance;
        private IResultView _currentResultView;

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

            // Clean up previous instance and its event subscriptions
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
            _currentViewInstance = _currentResultView as ScreenBase;

            if (_currentViewInstance != null)
            {
                // Bubble up events from the new instance
                _currentResultView.OnRematchClicked += OnRematchClicked;
                _currentResultView.OnMainMenuClicked += OnMainMenuClicked;

                // Tell the view to show itself and start its animation sequence.
                _currentResultView.ShowAndPlaySequence(resultDto, personalBest, playerRank, totalPlayers);
            }
        }

        private IResultView InstantiateView(GameResultDto dto, float personalBest)
        {
            bool isSinglePlayer = dto.FinalPlayerDatas.Length == 1;

            if (isSinglePlayer)
            {
                bool isNewRecord = dto.FinalGameTime > personalBest && personalBest > 0;
                if (isNewRecord)
                {
                    return Instantiate(_spNewRecordPrefab, transform);
                }
                else
                {
                    return Instantiate(_spNormalPrefab, transform);
                }
            }
            else // Multiplayer
            {
                bool isRanked = dto.NewWinnerRating != 0 || dto.NewLoserRating != 0;
                if (isRanked)
                {
                    return Instantiate(_rankedPrefab, transform);
                }
                else
                {
                    return Instantiate(_freePrefab, transform);
                }
            }
        }
    }
}