using UnityEngine;
using TMPro;
using TypingSurvivor.Features.UI.Common;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    /// <summary>
    /// タイトル画面のUIイベントと表示更新を担当します。
    /// </summary>
    public class TitleScreenController : ScreenBase
    {
        [SerializeField] private InteractiveButton _startButton;
        [SerializeField] private InteractiveButton _switchProfileButton;
        [SerializeField] private TMP_Text _statusText;

        private UIFlowCoordinator _flowCoordinator;

        protected override void Awake()
        {
            base.Awake();
        }

        public void Initialize(UIFlowCoordinator coordinator)
        {
            _flowCoordinator = coordinator;
            _startButton.onClick.AddListener(OnStartButtonClicked);
            _switchProfileButton.onClick.AddListener(OnSwitchProfileButtonClicked);
        }

        private void OnDestroy()
        {
            _startButton.onClick.RemoveListener(OnStartButtonClicked);
            _switchProfileButton.onClick.RemoveListener(OnSwitchProfileButtonClicked);
        }

        private void OnStartButtonClicked()
        {
            _flowCoordinator.OnTitleScreenAction();
        }

        private void OnSwitchProfileButtonClicked()
        {
            _flowCoordinator.RequestStateChange(UIFlowCoordinator.PlayerUIState.SelectingProfile);
        }

        public void UpdateView(string status, bool isStartButtonInteractable, bool isSwitchProfileButtonVisible)
        {
            if(_statusText != null) _statusText.text = status;
            if(_startButton != null) _startButton.interactable = isStartButtonInteractable;
            if(_switchProfileButton != null) _switchProfileButton.gameObject.SetActive(isSwitchProfileButtonVisible);
        }
    }
}
