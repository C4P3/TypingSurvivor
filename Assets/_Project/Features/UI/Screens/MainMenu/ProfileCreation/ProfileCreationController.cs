using UnityEngine;
using TMPro;
using TypingSurvivor.Features.UI.Common;
using TypingSurvivor.Features.Core.App;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    /// <summary>
    /// プレイヤー名入力画面のロジックを担当します。ScreenBaseを継承して自身の表示/非表示を管理します。
    /// </summary>
    public class ProfileCreationController : ScreenBase
    {
        [SerializeField] private TMP_InputField _nameInputField;
        [SerializeField] private InteractiveButton _confirmButton;
        [SerializeField] private TMP_Text _errorText;

        private UIFlowCoordinator _flowCoordinator;
        private AppManager _appManager;

        protected override void Awake()
        {
            base.Awake();
            // この画面固有のAwake処理があればここに記述
        }

        /// <summary>
        /// UIFlowCoordinatorによって初期化されます。
        /// </summary>
        public void Initialize(UIFlowCoordinator coordinator, AppManager appManager)
        {
            _flowCoordinator = coordinator;
            _appManager = appManager;
            _confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            if(_errorText != null) _errorText.gameObject.SetActive(false);
        }
        
        private void OnDestroy()
        {
            _confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
        }

        private async void OnConfirmButtonClicked()
        {
            _confirmButton.interactable = false;
            if(_errorText != null) _errorText.gameObject.SetActive(false);

            string playerName = _nameInputField.text;

            // 簡単な入力値検証
            if (string.IsNullOrWhiteSpace(playerName) || playerName.Length < 3)
            {
                if(_errorText != null)
                {
                    _errorText.text = "Please enter at least 3 characters.";
                    _errorText.gameObject.SetActive(true);
                }
                _confirmButton.interactable = true;
                return;
            }

            // プレイヤー名を保存 (CloudSaveServiceにメソッドがある想定)
            // bool success = await _appManager.CloudSaveService.SavePlayerName(playerName);
            bool success = true; // 仮実装：保存は必ず成功する

            if (success)
            {
                // 成功したらUIFlowCoordinatorに通知
                _flowCoordinator.OnProfileCreated();
            }
            else
            {
                if(_errorText != null)
                {
                    _errorText.text = "Failed to save name. Please try again.";
                    _errorText.gameObject.SetActive(true);
                }
                _confirmButton.interactable = true;
            }
        }
    }
}

