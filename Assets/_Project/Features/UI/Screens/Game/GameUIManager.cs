using System.Collections;
using System.Collections.Generic;
using TypingSurvivor.Features.Core.Audio;
using TypingSurvivor.Features.Core.PlayerStatus;
using TypingSurvivor.Features.Core.VFX;
using TypingSurvivor.Features.Game.Camera;
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
    /// It listens to game state changes and tells the UIManager to show/hide the appropriate screens.
    /// </summary>
    [RequireComponent(typeof(UIManager))]
    public class GameUIManager : MonoBehaviour
    {
        [Header("UI System")]
        [SerializeField] private UIManager _uiManager;

        [Header("Screen References")]
        [SerializeField] private InGameHUDManager _inGameHUD;
        [SerializeField] private ResultScreen _resultScreen;
        [SerializeField] private CountdownScreen _countdownScreen;

        [Header("Low Oxygen Effect")]
        [SerializeField] private float _lowOxygenPitch = 1.2f;
        
        // --- Dependencies ---
        private IGameStateReader _gameStateReader;
        private IPlayerStatusSystemReader _playerStatusReader;
        private GameManager _gameManager; // For sending RPCs
        private CameraManager _cameraManager; // Injected dependency

        // --- State ---
        private readonly Dictionary<ulong, Coroutine> _blinkingCoroutines = new();
        private readonly Dictionary<ulong, LowHealthEffect> _activeLowHealthEffects = new();
        private bool _showDisconnectGUI = false;

        protected virtual void Awake()
        {
            if (_uiManager == null)
            {
                _uiManager = GetComponent<UIManager>();
            }
        }

        public void Initialize(IGameStateReader gameStateReader, IPlayerStatusSystemReader playerStatusReader, GameManager gameManager, TypingSurvivor.Features.Game.Typing.ITypingService typingService, CameraManager cameraManager)
        {
            _gameStateReader = gameStateReader;
            _playerStatusReader = playerStatusReader;
            _gameManager = gameManager;
            _cameraManager = cameraManager;

            _inGameHUD.Initialize(gameStateReader, playerStatusReader, typingService);

            SubscribeToEvents();

            // Set initial UI state
            HandlePhaseChanged(default, _gameStateReader.CurrentPhaseNV.Value);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            _gameStateReader.CurrentPhaseNV.OnValueChanged += HandlePhaseChanged;
            _resultScreen.OnRematchClicked += HandleRematchClicked;
            _resultScreen.OnMainMenuClicked += HandleMainMenuClicked;
            _gameManager.OnLowOxygenStateChanged_Client += HandleLowOxygenStateChange;

            if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_gameStateReader != null) _gameStateReader.CurrentPhaseNV.OnValueChanged -= HandlePhaseChanged;
            if (_resultScreen != null) 
            {
                _resultScreen.OnRematchClicked -= HandleRematchClicked;
                _resultScreen.OnMainMenuClicked -= HandleMainMenuClicked;
            }
            if (_gameManager != null) _gameManager.OnLowOxygenStateChanged_Client -= HandleLowOxygenStateChange;
            if (NetworkManager.Singleton != null) NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }

        private void HandlePhaseChanged(GamePhase previousPhase, GamePhase newPhase)
        {
            switch (newPhase)
            {
                case GamePhase.WaitingForPlayers:
                    _uiManager.ShowScreen(null); // Hide all screens
                    break;

                case GamePhase.Countdown:
                    _uiManager.ShowScreen(_countdownScreen);
                    _countdownScreen.StartCountdown(() => {}); // Countdown is now self-managed
                    break;

                case GamePhase.Playing:
                    _uiManager.ShowScreen(_inGameHUD);
                    break;

                case GamePhase.Finished:
                    ResetLowOxygenEffects();
                    _uiManager.ShowScreen(_resultScreen);
                    _resultScreen.Show("Game Over!"); // Pass the message
                    break;
            }
        }

        #region Event Handlers

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
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                _showDisconnectGUI = true;
            }
        }

        #endregion

        #region Specific UI Logic (Could be refactored into own controllers)

        private void ResetLowOxygenEffects()
        {
            foreach (var coroutine in _blinkingCoroutines.Values) StopCoroutine(coroutine);
            _blinkingCoroutines.Clear();

            foreach (var effect in _activeLowHealthEffects.Values) if (effect != null) effect.SetOpacity(0f);
            _activeLowHealthEffects.Clear();
            
            MusicManager.Instance.ResetPitch();
        }

        public void HandleLowOxygenStateChange(ulong clientId, bool isLowOxygen)
        {
            var playerCamera = _cameraManager.GetCameraForPlayer(clientId);
            if (playerCamera == null) return;

            var lowHealthEffect = playerCamera.GetComponent<LowHealthEffect>();
            if (lowHealthEffect == null) return;

            if (isLowOxygen)
            {
                if (!_blinkingCoroutines.ContainsKey(clientId))
                {
                    Coroutine coroutine = StartCoroutine(BlinkEffectCoroutine(lowHealthEffect));
                    _blinkingCoroutines[clientId] = coroutine;
                    _activeLowHealthEffects[clientId] = lowHealthEffect;
                }
            }
            else
            {
                if (_blinkingCoroutines.TryGetValue(clientId, out Coroutine coroutine)) StopCoroutine(coroutine);
                _blinkingCoroutines.Remove(clientId);
                if (_activeLowHealthEffects.TryGetValue(clientId, out var effect)) effect.SetOpacity(0f);
                _activeLowHealthEffects.Remove(clientId);
            }

            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                if (isLowOxygen) MusicManager.Instance.SetPitch(_lowOxygenPitch);
                else MusicManager.Instance.ResetPitch();
            }
        }

        private IEnumerator BlinkEffectCoroutine(LowHealthEffect effect)
        {
            while (true)
            {
                float t = 0;
                while (t < 1) { t += Time.deltaTime * 2; effect.SetOpacity(Mathf.Lerp(0, 1f, t)); yield return null; }
                t = 0;
                while (t < 1) { t += Time.deltaTime * 2; effect.SetOpacity(Mathf.Lerp(1f, 0, t)); yield return null; }
            }
        }

        private void ReturnToMainMenu()
        {
            NetworkManager.Singleton.Shutdown();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        private void OnGUI()
        {
            if (!_showDisconnectGUI) return;

            float boxWidth = 300, boxHeight = 120;
            float screenWidth = Screen.width, screenHeight = Screen.height;
            Rect boxRect = new Rect((screenWidth - boxWidth) / 2, (screenHeight - boxHeight) / 2, boxWidth, boxHeight);
            GUI.Box(boxRect, "サーバーとの接続が切断されました。");
            float buttonWidth = 200, buttonHeight = 40;
            Rect buttonRect = new Rect(boxRect.x + (boxWidth - buttonWidth) / 2, boxRect.y + 60, buttonWidth, buttonHeight);
            if (GUI.Button(buttonRect, "メインメニューへ戻る"))
            {
                _showDisconnectGUI = false;
                ReturnToMainMenu();
            }
        }

        #endregion
    }
}
