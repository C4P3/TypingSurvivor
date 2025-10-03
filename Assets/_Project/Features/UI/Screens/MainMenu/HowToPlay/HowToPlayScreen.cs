using TypingSurvivor.Features.UI.Common;
using TypingSurvivor.Features.UI.Screens.MainMenu;
using UnityEngine;

namespace TypingSurvivor.Features.UI.Screens
{
    public class HowToPlayScreen : ScreenBase
    {
        [SerializeField] private InteractiveButton _backButton;

        private UIFlowCoordinator _flowCoordinator;

        public void Initialize(UIFlowCoordinator coordinator)
        {
            _flowCoordinator = coordinator;
            _backButton.onClick.AddListener(OnBackButtonClicked);
        }

        private void OnBackButtonClicked()
        {
            _flowCoordinator.RequestStateChange(UIFlowCoordinator.PlayerUIState.InMainMenu);
        }

        private void OnDestroy()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(OnBackButtonClicked);
            }
        }
    }
}
