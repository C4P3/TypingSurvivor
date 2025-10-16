using TypingSurvivor.Features.Core.App;
using TypingSurvivor.Features.Core.Leaderboard;
using TypingSurvivor.Features.UI.Common;
using TypingSurvivor.Features.UI.Screens.MainMenu;
using UnityEngine;
using UnityEngine.UI;

namespace TypingSurvivor.Features.UI.Screens
{
    public class RankingScreen : ScreenBase
    {
        [Header("Navigation")]
        [SerializeField] private InteractiveButton _backButton;

        [Header("Tabs")]
        [SerializeField] private InteractiveButton _ratingTabButton;
        [SerializeField] private InteractiveButton _survivalTabButton;

        [Header("Content Panels")]
        [SerializeField] private RatingRankingPanel _ratingPanel;
        [SerializeField] private SurvivalRankingPanel _survivalPanel;

        private UIFlowCoordinator _flowCoordinator;

        private enum LeaderboardTab { Rating, Survival }

        public void Initialize(UIFlowCoordinator coordinator)
        {
            _flowCoordinator = coordinator;
            var ratingLeaderboardService = AppManager.Instance.RatingLeaderboardService;
            var survivalLeaderboardService = AppManager.Instance.SurvivalLeaderboardService;

            _ratingPanel.Initialize(ratingLeaderboardService, survivalLeaderboardService);
            _survivalPanel.Initialize(ratingLeaderboardService, survivalLeaderboardService);

            _backButton.onClick.AddListener(OnBackButtonClicked);
            _ratingTabButton.onClick.AddListener(ShowRatingTab);
            _survivalTabButton.onClick.AddListener(ShowSurvivalTab);
        }

        public override void Show()
        {
            base.Show();
            // Default to showing the rating tab
            ShowRatingTab();
        }

        public override void Hide()
        {
            base.Hide();
            // Ensure panels are also hidden when the main screen is hidden
            _ratingPanel.Hide();
            _survivalPanel.Hide();
        }

        private void OnBackButtonClicked()
        {
            _flowCoordinator.RequestStateChange(UIFlowCoordinator.PlayerUIState.InMainMenu);
        }

        private void SwitchTab(LeaderboardTab tab)
        {
            if (tab == LeaderboardTab.Rating)
            {
                _ratingPanel.Show();
                _survivalPanel.Hide();
            }
            else // Survival
            {
                _survivalPanel.Show();
                _ratingPanel.Hide();
            }
        }

        private void ShowRatingTab() => SwitchTab(LeaderboardTab.Rating);
        private void ShowSurvivalTab() => SwitchTab(LeaderboardTab.Survival);

        private void OnDestroy()
        {
            if (_backButton != null) _backButton.onClick.RemoveListener(OnBackButtonClicked);
            if (_ratingTabButton != null) _ratingTabButton.onClick.RemoveListener(ShowRatingTab);
            if (_survivalTabButton != null) _survivalTabButton.onClick.RemoveListener(ShowSurvivalTab);
        }
    }
}

