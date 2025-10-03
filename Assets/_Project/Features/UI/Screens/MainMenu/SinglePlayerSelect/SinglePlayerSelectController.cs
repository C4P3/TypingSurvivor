using UnityEngine;
using TypingSurvivor.Features.UI.Common;
using TypingSurvivor.Features.Core.App;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    /// <summary>
    /// 一人用モードの難易度選択パネルを管理します。
    /// </summary>
    public class SinglePlayerSelectController : ScreenBase
    {
        [SerializeField] private InteractiveButton _easyButton;
        [SerializeField] private InteractiveButton _normalButton;
        [SerializeField] private InteractiveButton _hardButton;
        [SerializeField] private InteractiveButton _backButton;

        private UIFlowCoordinator _flowCoordinator;
        
        protected override void Awake()
        {
            base.Awake();
        }

        public void Initialize(UIFlowCoordinator coordinator)
        {
            _flowCoordinator = coordinator;
            _easyButton.onClick.AddListener(() => OnDifficultySelected(GameModeType.SinglePlayer)); // 仮のゲームモード
            _normalButton.onClick.AddListener(() => OnDifficultySelected(GameModeType.SinglePlayer));
            _hardButton.onClick.AddListener(() => OnDifficultySelected(GameModeType.SinglePlayer));
            _backButton.onClick.AddListener(OnBackButtonClicked);
        }

        private void OnDifficultySelected(GameModeType mode)
        {
            // TODO: 難易度に応じた設定を渡す
            _flowCoordinator.StartGame(mode);
        }

        private void OnBackButtonClicked()
        {
            _flowCoordinator.CloseCurrentPanel();
        }

        private void OnDestroy()
        {
            _easyButton.onClick.RemoveAllListeners();
            _normalButton.onClick.RemoveAllListeners();
            _hardButton.onClick.RemoveAllListeners();
            _backButton.onClick.RemoveAllListeners();
        }
    }
}
