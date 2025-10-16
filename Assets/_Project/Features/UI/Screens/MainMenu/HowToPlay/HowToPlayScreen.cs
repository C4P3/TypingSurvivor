using TypingSurvivor.Features.UI.Common;
using TypingSurvivor.Features.UI.Screens.MainMenu;
using UnityEngine;
using UnityEngine.UI;

namespace TypingSurvivor.Features.UI.Screens
{
    public class HowToPlayScreen : ScreenBase
    {
        [Header("Navigation")]
        [SerializeField] private InteractiveButton _backButton;

        [Header("Tabs")]
        [SerializeField] private InteractiveButton _controlsButton;
        [SerializeField] private InteractiveButton _singlePlayerButton;
        [SerializeField] private InteractiveButton _multiplayerButton;

        [Header("Content Panels")]
        [SerializeField] private ScreenBase _controlsPanel;
        [SerializeField] private ScreenBase _singlePlayerPanel;
        [SerializeField] private ScreenBase _multiplayerPanel;

        private UIFlowCoordinator _flowCoordinator;

        private enum HowToPlayTab { Controls, SinglePlayer, Multiplayer }

        public void Initialize(UIFlowCoordinator coordinator)
        {
            _flowCoordinator = coordinator;
            _backButton.onClick.AddListener(OnBackButtonClicked);
            _controlsButton.onClick.AddListener(ShowControlsTab);
            _singlePlayerButton.onClick.AddListener(ShowSinglePlayerTab);
            _multiplayerButton.onClick.AddListener(ShowMultiplayerTab);
        }

        public override void Show()
        {
            base.Show();
            // Default to showing the controls tab
            ShowControlsTab();
        }

        private void OnBackButtonClicked()
        {
            _flowCoordinator.RequestStateChange(UIFlowCoordinator.PlayerUIState.InMainMenu);
        }

        private void SwitchTab(HowToPlayTab tab)
        {
            if (tab == HowToPlayTab.Controls) _controlsPanel.Show(); else _controlsPanel.Hide();
            if (tab == HowToPlayTab.SinglePlayer) _singlePlayerPanel.Show(); else _singlePlayerPanel.Hide();
            if (tab == HowToPlayTab.Multiplayer) _multiplayerPanel.Show(); else _multiplayerPanel.Hide();
        }

        private void ShowControlsTab() => SwitchTab(HowToPlayTab.Controls);
        private void ShowSinglePlayerTab() => SwitchTab(HowToPlayTab.SinglePlayer);
        private void ShowMultiplayerTab() => SwitchTab(HowToPlayTab.Multiplayer);

        private void OnDestroy()
        {
            if (_backButton != null) _backButton.onClick.RemoveListener(OnBackButtonClicked);
            if (_controlsButton != null) _controlsButton.onClick.RemoveListener(ShowControlsTab);
            if (_singlePlayerButton != null) _singlePlayerButton.onClick.RemoveListener(ShowSinglePlayerTab);
            if (_multiplayerButton != null) _multiplayerButton.onClick.RemoveListener(ShowMultiplayerTab);
        }
    }
}
