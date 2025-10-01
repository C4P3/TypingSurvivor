using UnityEngine;
using TypingSurvivor.Features.UI.Common;
using System.Collections.Generic;

namespace TypingSurvivor.Features.UI
{
    /// <summary>
    /// Manages screen and panel transitions using a base screen and a stack of overlay panels.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private ScreenBase _currentScreen;
        private readonly Stack<ScreenBase> _panelStack = new Stack<ScreenBase>();

        /// <summary>
        /// Hides all current screens and panels, then shows the new base screen.
        /// </summary>
        /// <param name="newScreen">The new base screen to show. Can be null to show nothing.</param>
        public void ShowScreen(ScreenBase newScreen)
        {
            // Hide all active panels first
            while (_panelStack.Count > 0)
            {
                PopPanel();
            }

            // Hide the current base screen
            if (_currentScreen != null)
            {
                _currentScreen.Hide();
            }

            // Show the new base screen
            if (newScreen != null)
            {
                newScreen.Show();
                _currentScreen = newScreen;
            }
            else
            {
                _currentScreen = null;
            }
        }

        /// <summary>
        /// Pushes a panel onto the overlay stack and shows it.
        /// </summary>
        /// <param name="panel">The panel to show as an overlay.</param>
        public void PushPanel(ScreenBase panel)
        {
            if (panel == null) return;

            panel.Show();
            _panelStack.Push(panel);
        }

        /// <summary>
        /// Hides and removes the most recent panel from the overlay stack.
        /// </summary>
        public void PopPanel()
        {
            if (_panelStack.Count > 0)
            {
                ScreenBase topPanel = _panelStack.Pop();
                topPanel.Hide();
            }
        }
    }
}
