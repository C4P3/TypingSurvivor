using System.Collections.Generic;
using TypingSurvivor.Features.Core.Audio;
using TypingSurvivor.Features.Core.PlayerStatus;
using TypingSurvivor.Features.Core.VFX;
using TypingSurvivor.Features.Game.Camera;
using TypingSurvivor.Features.Game.Gameplay;
using TypingSurvivor.Features.Game.Gameplay.Data;
using System.Linq;
using TypingSurvivor.Features.Game.Typing;
using TypingSurvivor.Features.UI.Screens;
using TypingSurvivor.Features.UI.Screens.InGameHUD;
using TypingSurvivor.Features.UI.Screens.Global; // Changed
using Unity.Netcode;
using UnityEngine;

namespace TypingSurvivor.Features.UI
{
    [RequireComponent(typeof(UIManager))]
    public class GameUIManager : MonoBehaviour
    {
        [Header("UI System")]
        [SerializeField] private UIManager _uiManager;

        [Header("Screen & Prefab References")]
        [SerializeField] private InGameHUDManager _inGameHUDPrefab; // Prefab to instantiate
        [SerializeField] private ResultScreen _resultScreen;
        [SerializeField] private CountdownScreen _countdownScreen;
        [SerializeField] private InGameGlobalView _inGameGlobalView; // Changed

        [Header("Low Oxygen Effect")]
        [SerializeField] private float _lowOxygenPitch = 1.2f;
        
        private IGameStateReader _gameStateReader;
        private IPlayerStatusSystemReader _playerStatusReader;
        private GameManager _gameManager;
        private ITypingService _typingService;
        private CameraManager _cameraManager;

        private readonly Dictionary<ulong, InGameHUDManager> _playerHuds = new();
        private NetworkList<NetworkObjectReference>.OnListChangedDelegate _onPlayerListChangedHandler;
        private readonly Dictionary<ulong, Coroutine> _blinkingCoroutines = new();
        private readonly Dictionary<ulong, LowHealthEffect> _activeLowHealthEffects = new();
        private bool _showDisconnectGUI = false;

        protected virtual void Awake()
        {
            if (_uiManager == null) _uiManager = GetComponent<UIManager>();
            _onPlayerListChangedHandler = OnPlayerListChanged;
            // Removed instantiation of typing display
        }

        public void Initialize(IGameStateReader gameStateReader, IPlayerStatusSystemReader playerStatusReader, GameManager gameManager, ITypingService typingService, CameraManager cameraManager)
        {
            _gameStateReader = gameStateReader;
            _playerStatusReader = playerStatusReader;
            _gameManager = gameManager;
            _typingService = typingService;
            _cameraManager = cameraManager;

            SubscribeToEvents();
            HandlePhaseChanged(default, _gameStateReader.CurrentPhaseNV.Value);
            UpdatePlayerHUDs(); // Initial HUD setup
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            foreach (var hud in _playerHuds.Values) if (hud != null) Destroy(hud.gameObject);
            _playerHuds.Clear();
        }

        private void SubscribeToEvents()
        {
            _gameStateReader.CurrentPhaseNV.OnValueChanged += HandlePhaseChanged;
            _gameStateReader.SpawnedPlayers.OnListChanged += _onPlayerListChangedHandler;
            _resultScreen.OnRematchClicked += HandleRematchClicked;
            _resultScreen.OnMainMenuClicked += HandleMainMenuClicked;
            _gameManager.OnLowOxygenStateChanged_Client += HandleLowOxygenStateChange;
            _gameManager.OnResultReceived_Client += HandleResultReceived;
            _cameraManager.OnCameraAssigned += HandleCameraAssigned;

            if (_typingService != null)
            {
                _typingService.OnTypingProgressed += HandleTypingProgressed;
                _typingService.OnTypingCancelled += HandleTypingCancelled;
                _typingService.OnTypingSuccess += HandleTypingSuccess;
                _typingService.OnTypingMiss += HandleTypingMiss;
            }

            if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_gameStateReader != null)
            {
                _gameStateReader.CurrentPhaseNV.OnValueChanged -= HandlePhaseChanged;
                _gameStateReader.SpawnedPlayers.OnListChanged -= _onPlayerListChangedHandler;
            }
            if (_resultScreen != null) 
            {
                _resultScreen.OnRematchClicked -= HandleRematchClicked;
                _resultScreen.OnMainMenuClicked -= HandleMainMenuClicked;
            }
            if (_gameManager != null)
            {
                _gameManager.OnLowOxygenStateChanged_Client -= HandleLowOxygenStateChange;
                _gameManager.OnResultReceived_Client -= HandleResultReceived;
            }
            if (_cameraManager != null) _cameraManager.OnCameraAssigned -= HandleCameraAssigned;
            
            if (_typingService != null)
            {
                _typingService.OnTypingProgressed -= HandleTypingProgressed;
                _typingService.OnTypingCancelled -= HandleTypingCancelled;
                _typingService.OnTypingSuccess -= HandleTypingSuccess;
                _typingService.OnTypingMiss -= HandleTypingMiss;
            }

            if (NetworkManager.Singleton != null) NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }

        private void HandleCameraAssigned(ulong clientId, UnityEngine.Camera camera)
        {
            if (_playerHuds.TryGetValue(clientId, out var hud))
            {
                hud.SetRenderCamera(camera);
            }
        }

        private void OnPlayerListChanged(Unity.Netcode.NetworkListEvent<Unity.Netcode.NetworkObjectReference> changeEvent)
        {
            UpdatePlayerHUDs();
        }

        private void UpdatePlayerHUDs()
        {
            var activePlayerIds = new HashSet<ulong>();
            foreach (var playerRef in _gameStateReader.SpawnedPlayers)
            {
                if (playerRef.TryGet(out var netObj))
                {
                    activePlayerIds.Add(netObj.OwnerClientId);
                }
            }

            var hudsToRemove = _playerHuds.Keys.Where(id => !activePlayerIds.Contains(id)).ToList();
            foreach (var id in hudsToRemove)
            {
                if (_playerHuds.TryGetValue(id, out var hud))
                {
                    Destroy(hud.gameObject);
                }
                _playerHuds.Remove(id);
            }

            foreach (var playerRef in _gameStateReader.SpawnedPlayers)
            {
                if (playerRef.TryGet(out var netObj))
                {
                    var clientId = netObj.OwnerClientId;
                    if (!_playerHuds.ContainsKey(clientId))
                    {
                        var newHud = Instantiate(_inGameHUDPrefab, transform);
                        newHud.gameObject.name = $"PlayerHUD_{clientId}";
                        
                        newHud.SetPlayerOwnerId(clientId);
                        newHud.Initialize(_gameStateReader, _playerStatusReader);
                        _playerHuds[clientId] = newHud;
                    }
                }
            }
            HandlePhaseChanged(default, _gameStateReader.CurrentPhaseNV.Value);
        }

        private void HandlePhaseChanged(GamePhase previousPhase, GamePhase newPhase)
        {
            foreach (var hud in _playerHuds.Values)
            {
                if (newPhase == GamePhase.Playing) hud.Show();
                else hud.Hide();
            }

            if (_inGameGlobalView != null && newPhase == GamePhase.Playing)
            {
                 _inGameGlobalView.Show();
            }
            else if (_inGameGlobalView != null)
            {
                _inGameGlobalView.Hide();
            }

            switch (newPhase)
            {
                case GamePhase.WaitingForPlayers:
                    _uiManager.ShowScreen(null);
                    break;
                case GamePhase.Countdown:
                    _uiManager.ShowScreen(_countdownScreen);
                    _countdownScreen.StartCountdown(() => {});
                    break;
                case GamePhase.Playing:
                    _uiManager.ShowScreen(null);
                    break;
                case GamePhase.Finished:
                    ResetLowOxygenEffects();
                    _uiManager.ShowScreen(_resultScreen);
                    // The showing of the result screen is now handled by HandleResultReceived
                    break;
            }
        }

        #region Typing Handlers

        private void HandleTypingSuccess()
        {
            SfxManager.Instance.PlaySfx(SoundId.TypingSuccess);
            if (_inGameGlobalView != null) _inGameGlobalView.SetTypingActive(false);
        }

        private void HandleTypingMiss()
        {
            SfxManager.Instance.PlaySfx(SoundId.TypingMiss);
        }

        private void HandleTypingProgressed()
        {
            if (_inGameGlobalView == null) return;

            if (_typingService.IsTyping)
            {
                _inGameGlobalView.SetTypingActive(true);
                _inGameGlobalView.UpdateTypingQuestion(_typingService.GetDisplayText());
                _inGameGlobalView.UpdateTypingInput(_typingService.GetTypedRomaji(), _typingService.GetRemainingRomaji());
                SfxManager.Instance.PlaySfx(SoundId.TypingKeyPress);
            }
            else
            {
                _inGameGlobalView.SetTypingActive(false);
            }
        }

        private void HandleTypingCancelled()
        {
            if (_inGameGlobalView != null) _inGameGlobalView.SetTypingActive(false);
        }

        #endregion

        #region Event Handlers

        private void HandleResultReceived(GameManager.GameResultDto resultDto)
        {
            _resultScreen.Show(resultDto);
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
            if (clientId == NetworkManager.Singleton.LocalClientId) _showDisconnectGUI = true;
        }

        #endregion

        #region Specific UI Logic

        private void ResetLowOxygenEffects()
        {
            foreach (var coroutine in _blinkingCoroutines.Values) StopCoroutine(coroutine);
            _blinkingCoroutines.Clear();

            foreach (var effect in _activeLowHealthEffects.Values) if (effect != null) effect.SetOpacity(0f);
            _activeLowHealthEffects.Clear();
            
            if(MusicManager.Instance != null) MusicManager.Instance.ResetPitch();
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
                    var coroutine = StartCoroutine(BlinkEffectCoroutine(lowHealthEffect));
                    _blinkingCoroutines[clientId] = coroutine;
                    _activeLowHealthEffects[clientId] = lowHealthEffect;
                }
            }
            else
            {
                if (_blinkingCoroutines.TryGetValue(clientId, out var coroutine)) StopCoroutine(coroutine);
                _blinkingCoroutines.Remove(clientId);
                if (_activeLowHealthEffects.TryGetValue(clientId, out var effect)) effect.SetOpacity(0f);
                _activeLowHealthEffects.Remove(clientId);
            }

            if (clientId == NetworkManager.Singleton.LocalClientId && MusicManager.Instance != null)
            {
                if (isLowOxygen) MusicManager.Instance.SetPitch(_lowOxygenPitch);
                else MusicManager.Instance.ResetPitch();
            }
        }

        private System.Collections.IEnumerator BlinkEffectCoroutine(LowHealthEffect effect)
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
            var boxRect = new Rect((screenWidth - boxWidth) / 2, (screenHeight - boxHeight) / 2, boxWidth, boxHeight);
            GUI.Box(boxRect, "サーバーとの接続が切断されました。");
            float buttonWidth = 200, buttonHeight = 40;
            var buttonRect = new Rect(boxRect.x + (boxWidth - buttonWidth) / 2, boxRect.y + 60, buttonWidth, buttonHeight);
            if (GUI.Button(buttonRect, "メインメニューへ戻る"))
            {
                _showDisconnectGUI = false;
                ReturnToMainMenu();
            }
        }

        #endregion
    }
}
