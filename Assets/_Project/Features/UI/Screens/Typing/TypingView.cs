using TMPro;
using UnityEngine;
using TypingSurvivor.Features.UI.Common;

namespace TypingSurvivor.Features.UI.Screens.Typing
{
    // This is the panel specifically for typing, which can be shown/hidden.
    public class TypingView : ScreenBase
    {
        [Header("Text Components")]
        [SerializeField] private TextMeshProUGUI _questionText;
        [SerializeField] private TextMeshProUGUI _typedText;
        [SerializeField] private TextMeshProUGUI _remainingText;

        public void UpdateQuestion(string question)
        {
            if (_questionText != null) _questionText.text = question;
        }

        public void UpdateInput(string typed, string remaining)
        {
            if (_typedText != null) _typedText.text = typed;
            if (_remainingText != null) _remainingText.text = remaining;
        }
    }
}
