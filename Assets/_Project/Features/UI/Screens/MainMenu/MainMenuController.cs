using UnityEngine;
using TypingSurvivor.Features.UI.Common;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    /// <summary>
    /// メインメニューの各ボタンの役割をUIFlowCoordinatorに伝えることのみを担当します。
    /// </summary>
    public class MainMenuController : ScreenBase
    {
        [SerializeField] private InteractiveButton _singlePlayerButton;
        [SerializeField] private InteractiveButton _multiplayerButton;
        [SerializeField] private InteractiveButton _rankingButton;
        [SerializeField] private InteractiveButton _shopButton;
        [SerializeField] private InteractiveButton _settingsButton;
        [SerializeField] private InteractiveButton _howToPlayButton;

        private UIFlowCoordinator _flowCoordinator;

        protected override void Awake()
        {
            base.Awake();
        }

        public void Initialize(UIFlowCoordinator coordinator)
        {
            _flowCoordinator = coordinator;

            _singlePlayerButton.onClick.AddListener(() => _flowCoordinator.RequestStateChange(UIFlowCoordinator.PlayerUIState.SelectingSinglePlayer));
            _multiplayerButton.onClick.AddListener(() => _flowCoordinator.RequestStateChange(UIFlowCoordinator.PlayerUIState.SelectingMultiplayer));
            _rankingButton.onClick.AddListener(() => _flowCoordinator.RequestStateChange(UIFlowCoordinator.PlayerUIState.InRanking));
            _shopButton.onClick.AddListener(() => _flowCoordinator.RequestStateChange(UIFlowCoordinator.PlayerUIState.InShop));
            _settingsButton.onClick.AddListener(() => _flowCoordinator.RequestStateChange(UIFlowCoordinator.PlayerUIState.InSettings));
            _howToPlayButton.onClick.AddListener(() => _flowCoordinator.RequestStateChange(UIFlowCoordinator.PlayerUIState.InHowToPlay));
        }

        private void OnDestroy()
        {
            _singlePlayerButton.onClick.RemoveAllListeners();
            _multiplayerButton.onClick.RemoveAllListeners();
            _rankingButton.onClick.RemoveAllListeners();
            _shopButton.onClick.RemoveAllListeners();
            _settingsButton.onClick.RemoveAllListeners();
            _howToPlayButton.onClick.RemoveAllListeners();
        }
    }
}

