using UnityEngine;
using TypingSurvivor.Features.UI.Common;
using TypingSurvivor.Features.Core.App;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    /// <summary>
    /// マルチプレイヤーのマッチング方法選択パネルを管理します。
    /// </summary>
    public class MultiplayerSelectController : ScreenBase
    {
        [SerializeField] private InteractiveButton _freeMatchButton;
        [SerializeField] private InteractiveButton _rankedMatchButton;
        [SerializeField] private InteractiveButton _privateMatchButton;
        [SerializeField] private InteractiveButton _backButton;

        private UIFlowCoordinator _flowCoordinator;
        
        protected override void Awake()
        {
            base.Awake();
        }

        public void Initialize(UIFlowCoordinator coordinator)
        {
            _flowCoordinator = coordinator;
            _freeMatchButton.onClick.AddListener(OnFreeMatchClicked);
            _rankedMatchButton.onClick.AddListener(OnRankedMatchClicked);
            _privateMatchButton.onClick.AddListener(OnPrivateMatchClicked);
            _backButton.onClick.AddListener(OnBackButtonClicked);
        }

        private async void OnFreeMatchClicked()
        {
            await _flowCoordinator.StartPublicMatchmaking("free-match", GameModeType.MultiPlayer);
        }

        private async void OnRankedMatchClicked()
        {
            await _flowCoordinator.StartPublicMatchmaking("ranked-match", GameModeType.RankedMatch);
        }
        
        private void OnPrivateMatchClicked()
        {
            _flowCoordinator.RequestStateChange(UIFlowCoordinator.PlayerUIState.EnteringMatchCode);
        }

        private void OnBackButtonClicked()
        {
            _flowCoordinator.CloseCurrentPanel();
        }
        
        private void OnDestroy()
        {
            _freeMatchButton.onClick.RemoveAllListeners();
            _rankedMatchButton.onClick.RemoveAllListeners();
            _privateMatchButton.onClick.RemoveAllListeners();
            _backButton.onClick.RemoveAllListeners();
        }
    }
}
