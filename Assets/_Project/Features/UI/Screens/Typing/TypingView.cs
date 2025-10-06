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
        [SerializeField] private TextMeshProUGUI _romajiLineText; // Combined text field

        private const string HIGHLIGHT_COLOR = "#A5FF9B"; // A light green color

        protected override void Awake()
        {
            base.Awake();
            // Ensure Rich Text is enabled for the effect to work.
            if (_romajiLineText != null) _romajiLineText.richText = true;
        }

        public void UpdateQuestion(string question)
        {
            if (_questionText != null) _questionText.text = question;
        }

        public void UpdateInput(string typed, string remaining)
        {
            if (_romajiLineText != null)
            {
                _romajiLineText.text = $"<color={HIGHLIGHT_COLOR}>{typed}</color>{remaining}";
            }
        }
    }
}
