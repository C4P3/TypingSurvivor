using UnityEngine;
using TypingSurvivor.Features.UI.Common;
using System.Collections.Generic;
using System.Linq;

namespace TypingSurvivor.Features.UI
{
    /// <summary>
    /// Manages screen and panel transitions, including overlays.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private ScreenBase _currentScreen;
        private readonly List<ScreenBase> _overlayStack = new List<ScreenBase>();

        /// <summary>
        /// Hides all current screens and overlays, then shows the new screen.
        /// </summary>
        /// <param name="newScreen">The new base screen to show. Can be null to show nothing.</param>
        public void ShowScreen(ScreenBase newScreen)
        {
            // Hide all active overlays first
            while (_overlayStack.Count > 0)
            {
                HideTopOverlay();
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
        /// Shows a panel as an overlay on top of the current screen and other overlays.
        /// </summary>
        /// <param name="panel">The panel to show.</param>
        public void ShowPanelOverlay(ScreenBase panel)
        {
            if (panel == null || _overlayStack.Contains(panel)) return;

            panel.Show();
            _overlayStack.Add(panel);
        }

        /// <summary>
        /// Hides the most recently shown overlay panel.
        /// </summary>
        public void HideTopOverlay()
        {
            if (_overlayStack.Count > 0)
            {
                ScreenBase topPanel = _overlayStack.Last();
                _overlayStack.RemoveAt(_overlayStack.Count - 1);
                topPanel.Hide();
            }
        }
    }
}
