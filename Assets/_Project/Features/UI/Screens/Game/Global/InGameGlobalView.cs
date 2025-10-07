using TMPro;
using UnityEngine;
using TypingSurvivor.Features.UI.Common;
using TypingSurvivor.Features.UI.Screens.Typing;

namespace TypingSurvivor.Features.UI.Screens.Global
{
    public class InGameGlobalView : ScreenBase
    {
        [Header("Child Views")]
        [SerializeField] private TypingView _typingView;
        [SerializeField] private TextMeshProUGUI _timerText;

        private IGameStateReader _gameStateReader;

        public void Initialize(IGameStateReader gameStateReader)
        {
            _gameStateReader = gameStateReader;
            _gameStateReader.GameTimer.OnValueChanged += HandleTimerChanged;
            HandleTimerChanged(0, _gameStateReader.GameTimer.Value); // Initialize text
        }

        private void OnDestroy()
        {
            if (_gameStateReader != null)
            {
                _gameStateReader.GameTimer.OnValueChanged -= HandleTimerChanged;
            }
        }

        private void HandleTimerChanged(float previousValue, float newValue)
        {
            if (_timerText == null) return;

            int minutes = Mathf.FloorToInt(newValue / 60F);
            int seconds = Mathf.FloorToInt(newValue - minutes * 60);
            _timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        public void SetTypingActive(bool isActive)
        {
            if (_typingView == null) return;

            if (isActive)
            {
                _typingView.Show();
            }
            else
            {
                _typingView.Hide();
            }
        }

        public void UpdateTypingQuestion(string question)
        {
            if (_typingView != null) _typingView.UpdateQuestion(question);
        }

        public void UpdateTypingInput(string typed, string remaining)
        {
            if (_typingView != null) _typingView.UpdateInput(typed, remaining);
        }
    }
}