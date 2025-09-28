using TMPro;
using UnityEngine;

namespace TypingSurvivor.Features.UI.Screens.InGameHUD
{
    /// <summary>
    /// Manages the UI elements for displaying the typing challenge.
    /// This is a passive view component controlled by InGameHUDManager.
    /// </summary>
    public class TypingView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _typedText;
        [SerializeField] private TextMeshProUGUI _remainingText;
        [SerializeField] private GameObject _typingPanel; // The parent panel to show/hide

        private void Awake()
        {
            // Start hidden
            Hide();
        }

        /// <summary>
        /// Updates the displayed text.
        /// </summary>
        /// <param name="typed">The part of the text the user has already typed.</param>
        /// <param name="remaining">The part of the text the user still needs to type.</param>
        public void UpdateView(string typed, string remaining)
        {
            if (_typedText != null)
            {
                _typedText.text = typed;
            }
            if (_remainingText != null)
            {
                _remainingText.text = remaining;
            }
        }

        /// <summary>
        /// Shows the typing UI panel.
        /// </summary>
        public void Show()
        {
            if (_typingPanel != null)
            {
                _typingPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the typing UI panel.
        /// </summary>
        public void Hide()
        {
            if (_typingPanel != null)
            {
                _typingPanel.SetActive(false);
            }
        }
    }
}
