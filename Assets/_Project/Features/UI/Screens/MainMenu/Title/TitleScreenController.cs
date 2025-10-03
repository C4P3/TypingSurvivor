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
        [SerializeField] private InteractiveButton _actionButton;
        [SerializeField] private TMP_Text _statusText;

        private UIFlowCoordinator _flowCoordinator;

        protected override void Awake()
        {
            base.Awake();
        }

        public void Initialize(UIFlowCoordinator coordinator)
        {
            _flowCoordinator = coordinator;
            _actionButton.onClick.AddListener(OnActionButtonClicked);
        }

        private void OnDestroy()
        {
            _actionButton.onClick.RemoveListener(OnActionButtonClicked);
        }

        private void OnActionButtonClicked()
        {
            _flowCoordinator.OnTitleScreenAction();
        }

        public void UpdateView(string status, bool interactable)
        {
            if(_statusText != null) _statusText.text = status;
            if(_actionButton != null) _actionButton.interactable = interactable;
        }
    }
}

