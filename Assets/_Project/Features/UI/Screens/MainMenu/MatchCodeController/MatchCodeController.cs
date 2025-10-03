using UnityEngine;
using TMPro;
using TypingSurvivor.Features.UI.Common;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    /// <summary>
    /// 合言葉（ルームコード）入力パネルを管理します。
    /// </summary>
    public class MatchCodeController : ScreenBase
    {
        [SerializeField] private TMP_InputField _roomCodeInputField;
        [SerializeField] private InteractiveButton _joinButton;
        [SerializeField] private InteractiveButton _backButton;
        [SerializeField] private TMP_Text _errorText;
        
        private UIFlowCoordinator _flowCoordinator;
        
        protected override void Awake()
        {
            base.Awake();
        }

        public void Initialize(UIFlowCoordinator coordinator)
        {
            _flowCoordinator = coordinator;
            _joinButton.onClick.AddListener(OnJoinButtonClicked);
            _backButton.onClick.AddListener(OnBackButtonClicked);
            if (_errorText != null) _errorText.gameObject.SetActive(false);
        }

        public override void Show()
        {
            base.Show();
            // パネル表示時にエラーテキストを非表示にし、入力フィールドをクリアする
            if (_errorText != null) _errorText.gameObject.SetActive(false);
            _roomCodeInputField.text = "";
        }

        private void OnJoinButtonClicked()
        {
            string roomCode = _roomCodeInputField.text;
            if (string.IsNullOrWhiteSpace(roomCode))
            {
                if (_errorText != null)
                {
                    _errorText.text = "Please enter a room code.";
                    _errorText.gameObject.SetActive(true);
                }
                return;
            }
            
            _flowCoordinator.StartPrivateMatchmaking(roomCode.ToUpper());
        }

        private void OnBackButtonClicked()
        {
            _flowCoordinator.CloseCurrentPanel();
        }
        
        private void OnDestroy()
        {
            _joinButton.onClick.RemoveAllListeners();
            _backButton.onClick.RemoveAllListeners();
        }
    }
}
