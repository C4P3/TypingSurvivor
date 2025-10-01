
using UnityEngine;
using TypingSurvivor.Features.UI.Common;

namespace TypingSurvivor.Features.UI
{
    /// <summary>
    /// A simple UI manager responsible for orchestrating screen transitions.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private ScreenBase _currentScreen;

        /// <summary>
        /// Hides the current screen (if any) and shows the new one.
        /// </summary>
        /// <param name="newScreen">The screen to transition to.</param>
        public void ShowScreen(ScreenBase newScreen)
        {
            if (_currentScreen != null)
            {
                _currentScreen.Hide();
            }

            if (newScreen != null)
            {
                newScreen.Show();
                _currentScreen = newScreen;
            }
        }
    }
}
