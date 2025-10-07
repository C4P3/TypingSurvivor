using UnityEngine;
using TMPro;
using System;
using TypingSurvivor.Features.UI.Common;
using static TypingSurvivor.Features.Game.Gameplay.GameManager;
using Unity.Netcode;
using System.Text;

namespace TypingSurvivor.Features.UI.Screens
{
    /// <summary>
    /// Displays the game result and provides options for rematch or returning to the main menu.
    /// Inherits from ScreenBase to get fade in/out functionality.
    /// </summary>
    public class ResultScreen : ScreenBase
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _resultText; // e.g., "YOU WIN"
        [SerializeField] private TextMeshProUGUI _statsText; // For detailed stats
        [SerializeField] private InteractiveButton _rematchButton;
        [SerializeField] private InteractiveButton _mainMenuButton;

        // [Header("Result Panel Prefabs")]
        // [SerializeField] private PlayerResultPanel _playerResultPanelPrefab; // Prefab for displaying individual player stats
        // [SerializeField] private Transform _playerResultsContainer; // Layout group to hold the panels

        public event Action OnRematchClicked;
        public event Action OnMainMenuClicked;

        protected override void Awake()
        {
            base.Awake();
            _rematchButton.onClick.AddListener(() => OnRematchClicked?.Invoke());
            _mainMenuButton.onClick.AddListener(() => OnMainMenuClicked?.Invoke());
            
            // Start hidden
            _canvasGroup.alpha = 0;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private void OnDestroy()
        {
            _rematchButton.onClick.RemoveAllListeners();
            _mainMenuButton.onClick.RemoveAllListeners();
        }

        public void Show(GameResultDto resultDto)
        {
            // 1. Determine Win/Loss/Draw for the local player
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            if (resultDto.IsDraw)
            {
                _resultText.text = "DRAW";
            }
            else if (resultDto.WinnerClientId == localClientId)
            {
                _resultText.text = "YOU WIN";
            }
            else
            {
                _resultText.text = "YOU LOSE";
            }

            // 2. Display detailed stats for all players
            // TODO: Replace this with prefab instantiation
            StringBuilder statsBuilder = new StringBuilder();
            foreach (var playerData in resultDto.FinalPlayerDatas)
            {
                statsBuilder.AppendLine($"--- Player {playerData.ClientId} ---");
                statsBuilder.AppendLine($"Score: {playerData.Score}");
                statsBuilder.AppendLine($"Blocks Destroyed: {playerData.BlocksDestroyed}");
                statsBuilder.AppendLine($"Typing Misses: {playerData.TypingMisses}");
                statsBuilder.AppendLine();
            }
            // Display rating changes for ranked matches
            if (resultDto.NewWinnerRating != 0 || resultDto.NewLoserRating != 0) // Assuming 0 means not a ranked match
            {
                statsBuilder.AppendLine("--- Rating ---");
                statsBuilder.AppendLine($"Winner's New Rating: {resultDto.NewWinnerRating}");
                statsBuilder.AppendLine($"Loser's New Rating: {resultDto.NewLoserRating}");
            }

            _statsText.text = statsBuilder.ToString();

            base.Show(); // Call the base class Show to trigger the fade-in
        }
    }
}
