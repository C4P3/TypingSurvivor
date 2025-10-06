using UnityEngine;
using TypingSurvivor.Features.UI.Common;
using TypingSurvivor.Features.UI.Screens.Typing;

namespace TypingSurvivor.Features.UI.Screens.Global
{
    /// <summary>
    /// The main overlay view for the game scene.
    /// It contains other views like the TypingView and will hold the Pause button.
    /// </summary>
    public class InGameGlobalView : ScreenBase
    {
        [Header("Child Views")]
        [SerializeField] private TypingView _typingView;
        // [SerializeField] private GameObject _pauseButton; // For future use

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