using UnityEngine;
using Unity.Netcode;
using TypingSurvivor.Features.Core.PlayerStatus;
using TypingSurvivor.Features.Game.Typing;
using TypingSurvivor.Features.Core.Audio; // Add this using directive

using TypingSurvivor.Features.UI.Common;

namespace TypingSurvivor.Features.UI.Screens.InGameHUD
{
    public class InGameHUDManager : ScreenBase
    {
        // 子オブジェクトなどから参照を設定するUI部品
        [SerializeField] private OxygenView _oxygenView;
        [SerializeField] private ScoreView _scoreView;
        [SerializeField] private TypingView _typingView; // Reference to the new TypingView

        // DIコンテナから注入される、読み取り専用のインターフェース
        private IGameStateReader _gameStateReader;
        private IPlayerStatusSystemReader _playerStatusReader;
        private ITypingService _typingService;


        // [Inject]の代わりに、外部から呼び出される公開の初期化メソッドを用意
        public void Initialize(IGameStateReader gameStateReader, IPlayerStatusSystemReader playerStatusReader, ITypingService typingService)
        {
            _gameStateReader = gameStateReader;
            _playerStatusReader = playerStatusReader;
            _typingService = typingService;

            // 依存関係が注入されたので、イベントの購読を開始する
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            // 必ず購読解除
            UnSubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (_gameStateReader != null)
            {
                _gameStateReader.OnOxygenChanged += OnOxygenChanged;
                _gameStateReader.OnScoreChanged += OnScoreChanged;
            }
            if (_typingService != null)
            {
                _typingService.OnTypingProgressed += HandleTypingProgressed;
                _typingService.OnTypingCancelled += HandleTypingCancelled; // Also handle cancellation
                _typingService.OnTypingSuccess += HandleTypingSuccess;
                _typingService.OnTypingMiss += HandleTypingMiss;
            }
        }
        private void UnSubscribeEvents()
        {
            if (_gameStateReader != null)
            {
                _gameStateReader.OnOxygenChanged -= OnOxygenChanged;
                _gameStateReader.OnScoreChanged -= OnScoreChanged;
            }
            if (_typingService != null)
            {
                _typingService.OnTypingProgressed -= HandleTypingProgressed;
                _typingService.OnTypingCancelled -= HandleTypingCancelled;
                _typingService.OnTypingSuccess -= HandleTypingSuccess;
                _typingService.OnTypingMiss -= HandleTypingMiss;
            }
        }

        private void HandleTypingSuccess()
        {
            SfxManager.Instance.PlaySfx(SoundId.TypingSuccess);
            if (_typingView != null)
            {
                _typingView.Hide();
            }
        }

        private void HandleTypingMiss()
        {
            SfxManager.Instance.PlaySfx(SoundId.TypingMiss);
        }


        private void HandleTypingProgressed()
        {
            if (_typingView == null) return;

            // Play sound for every successful key press
            SfxManager.Instance.PlaySfx(SoundId.TypingKeyPress);

            if (_typingService.IsTyping)
            {
                _typingView.Show();
                _typingView.UpdateView(_typingService.GetTypedRomaji(), _typingService.GetRemainingRomaji());
            }
            else
            {
                _typingView.Hide();
            }
        }

        private void HandleTypingCancelled()
        {
            if (_typingView != null)
            {
                _typingView.Hide();
            }
        }

        // イベントを受け取ったら、担当のUI部品に更新を指示する
        private void OnOxygenChanged(ulong clientId, float newOxygenValue)
        {
            // Guard against race conditions where events are received before Initialize is called.
            if (_playerStatusReader == null) return;

            // This HUD should only display the local player's stats.
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                _oxygenView.UpdateView(newOxygenValue, _playerStatusReader.GetStatValue(NetworkManager.Singleton.LocalClientId, PlayerStat.MaxOxygen));
            }
        }

        private void OnScoreChanged(int newScoreValue)
        {
            _scoreView.UpdateView(newScoreValue);
        }
    }
}