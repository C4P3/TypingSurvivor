using TypingSurvivor.Features.Core.PlayerStatus;
using TypingSurvivor.Features.Game.Gameplay;
using TypingSurvivor.Features.Game.Gameplay.Data;
using TypingSurvivor.Features.UI.Screens;
using TypingSurvivor.Features.UI.Screens.InGameHUD;
using Unity.Netcode;
using UnityEngine;

namespace TypingSurvivor.Features.UI
{
    /// <summary>
    /// Manages the overall UI state for the Game scene.
    /// It listens to game state changes and shows/hides the appropriate UI screens.
    /// </summary>
    public class GameUIManager : MonoBehaviour
    {
        [Header("Screen References")]
        [SerializeField] private InGameHUDManager _inGameHUD;
        [SerializeField] private ResultScreen _resultScreen;
        // TODO: Add references for CountdownUI, WaitingForPlayersUI etc.

        private IGameStateReader _gameStateReader;
        private IPlayerStatusSystemReader _playerStatusReader;
        private GameManager _gameManager; // For sending RPCs

        // --- For Client-Side Disconnection ---
        private bool _showDisconnectGUI = false;

        public void Initialize(IGameStateReader gameStateReader, IPlayerStatusSystemReader playerStatusReader, GameManager gameManager)
        {
            _gameStateReader = gameStateReader;
            _playerStatusReader = playerStatusReader;
            _gameManager = gameManager;

            // Initialize child UI components
            _inGameHUD.Initialize(_gameStateReader, _playerStatusReader);

            _gameStateReader.CurrentPhaseNV.OnValueChanged += HandlePhaseChanged;
            _resultScreen.OnRematchClicked += HandleRematchClicked;
            _resultScreen.OnMainMenuClicked += HandleMainMenuClicked;

            // Subscribe to client-side disconnect event
            if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            }

            // Set initial UI state
            HandlePhaseChanged(default, _gameStateReader.CurrentPhaseNV.Value);
        }

        private void OnDestroy()
        {
            if (_gameStateReader != null)
            {
                _gameStateReader.CurrentPhaseNV.OnValueChanged -= HandlePhaseChanged;
            }
            if (_resultScreen != null)
            {
                _resultScreen.OnRematchClicked -= HandleRematchClicked;
                _resultScreen.OnMainMenuClicked -= HandleMainMenuClicked;
            }
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            }
        }

        private void HandlePhaseChanged(GamePhase previousPhase, GamePhase newPhase)
        {
            // Hide all screens first
            _inGameHUD.gameObject.SetActive(false);
            _resultScreen.Hide();

            // Show the correct screen based on the new phase
            switch (newPhase)
            {
                case GamePhase.WaitingForPlayers:
                    // Show Waiting UI
                    break;
                case GamePhase.Countdown:
                    // Show Countdown UI
                    break;
                case GamePhase.Playing:
                    _inGameHUD.gameObject.SetActive(true);
                    break;
                case GamePhase.Finished:
                    // TODO: Get actual result message
                    _resultScreen.Show("Game Over!");
                    break;
            }
        }

        private void HandleRematchClicked()
        {
            _gameManager.RequestRematchServerRpc();
        }

        private void HandleMainMenuClicked()
        {
            ReturnToMainMenu();
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            // This is the client-side disconnect handler.
            // We only care about our own disconnection.
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                _showDisconnectGUI = true;
            }
        }

        private void ReturnToMainMenu()
        {
            // Disconnect and load main menu
            NetworkManager.Singleton.Shutdown();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        private void OnGUI()
        {
            if (!_showDisconnectGUI) return;

            // Simple GUI to show disconnection message and provide a way back to the main menu.
            float boxWidth = 300;
            float boxHeight = 120;
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            Rect boxRect = new Rect((screenWidth - boxWidth) / 2, (screenHeight - boxHeight) / 2, boxWidth, boxHeight);

            GUI.Box(boxRect, "サーバーとの接続が切断されました。");

            float buttonWidth = 200;
            float buttonHeight = 40;
            Rect buttonRect = new Rect(boxRect.x + (boxWidth - buttonWidth) / 2, boxRect.y + 60, buttonWidth, buttonHeight);

            if (GUI.Button(buttonRect, "メインメニューへ戻る"))
            {
                _showDisconnectGUI = false; // Prevent multiple clicks
                ReturnToMainMenu();
            }
        }
    }
}
