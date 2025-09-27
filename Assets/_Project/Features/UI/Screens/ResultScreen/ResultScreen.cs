using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace TypingSurvivor.Features.UI.Screens
{
    /// <summary>
    /// Displays the game result and provides options for rematch or returning to the main menu.
    /// This component is a passive view, controlled by a manager class (e.g., GameUIManager).
    /// </summary>
    public class ResultScreen : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private Button _rematchButton;
        [SerializeField] private Button _mainMenuButton;

        public event Action OnRematchClicked;
        public event Action OnMainMenuClicked;

        private void Awake()
        {
            _rematchButton.onClick.AddListener(() => OnRematchClicked?.Invoke());
            _mainMenuButton.onClick.AddListener(() => OnMainMenuClicked?.Invoke());
        }

        private void OnDestroy()
        {
            _rematchButton.onClick.RemoveAllListeners();
            _mainMenuButton.onClick.RemoveAllListeners();
        }

        public void Show(string resultMessage)
        {
            _resultText.text = resultMessage;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
