using TypingSurvivor.Features.Core.App;
using TypingSurvivor.Features.Core.Matchmaking;
using TypingSurvivor.Features.UI.Common;
using UnityEngine;
using System.Collections;
using System.Threading.Tasks;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    public class MatchmakingController : MonoBehaviour
    {
        private MatchmakingService _matchmakingService;
        private UIManager _uiManager;
        private AppManager _appManager;
        private bool _isPrivateMatch;
        private GameModeType _currentGameMode;

        [Header("UI Panels")]
        [SerializeField] private MatchmakingWaitController _publicWaitPanel;
        [SerializeField] private PrivateLobbyWaitController _privateLobbyWaitPanel;

        public void Initialize(MatchmakingService matchmakingService, UIManager uiManager, AppManager appManager)
        {
            _matchmakingService = matchmakingService;
            _uiManager = uiManager;
            _appManager = appManager;
            
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (_matchmakingService == null) return;
            _matchmakingService.OnMatchSuccess += HandleMatchSuccess;
            _matchmakingService.OnMatchFailure += HandleMatchFailure;
            _matchmakingService.OnStatusUpdated += HandleStatusUpdate;

            _publicWaitPanel.OnCancelClicked += Cancel;
            _privateLobbyWaitPanel.OnCancelClicked += Cancel;
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void UnsubscribeFromEvents()
        {
            if (_matchmakingService == null) return;
            _matchmakingService.OnMatchSuccess -= HandleMatchSuccess;
            _matchmakingService.OnMatchFailure -= HandleMatchFailure;
            _matchmakingService.OnStatusUpdated -= HandleStatusUpdate;

            if(_publicWaitPanel != null) _publicWaitPanel.OnCancelClicked -= Cancel;
            if(_privateLobbyWaitPanel != null) _privateLobbyWaitPanel.OnCancelClicked -= Cancel;
        }

        public async Task StartPublicMatchmaking(string queueName, GameModeType gameMode)
        {
            _isPrivateMatch = false;
            _currentGameMode = gameMode;
            _uiManager.PushPanel(_publicWaitPanel);
            _publicWaitPanel.UpdateStatus("Searching for a match...");
            await _matchmakingService.CreateTicketAsync(queueName);
        }

        public async Task StartPrivateMatchmaking(string roomCode)
        {
            _isPrivateMatch = true;
            if (string.IsNullOrEmpty(roomCode))
            {
                Debug.LogError("Room code cannot be empty.");
                return;
            }
            _uiManager.PushPanel(_privateLobbyWaitPanel);
            _privateLobbyWaitPanel.ShowWithRoomCode(roomCode);
            await _matchmakingService.CreateTicketAsync("PrivateQueue", roomCode); // Using a placeholder queue name
        }

        public void Cancel()
        {
            _matchmakingService.CancelMatchmaking();
        }

        private void HandleMatchSuccess(MatchmakingResult result)
        {
            string status = "Match Found! Connecting...";
            if (_isPrivateMatch) _privateLobbyWaitPanel.UpdateStatus(status);
            else _publicWaitPanel.UpdateStatus(status);
            
            StartCoroutine(ConnectAfterDelay(result, 1.5f));
        }

        private IEnumerator ConnectAfterDelay(MatchmakingResult result, float delay)
        {
            yield return new WaitForSeconds(delay);
            Debug.Log($"Match found! Connecting to {result.Ip}:{result.Port}");
            _appManager.StartClient(result.Ip, (ushort)result.Port, _currentGameMode);
        }

        private void HandleMatchFailure(string reason)
        {
            string status = $"Failed: {reason}";
            if (_isPrivateMatch) _privateLobbyWaitPanel.UpdateStatus(status);
            else _publicWaitPanel.UpdateStatus(status);
            
            StartCoroutine(ClosePanelAfterDelay(2.5f));
        }

        private IEnumerator ClosePanelAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _uiManager.PopPanel();
        }

        private void HandleStatusUpdate(string status)
        {            
            if (_isPrivateMatch) _privateLobbyWaitPanel.UpdateStatus(status);
            else _publicWaitPanel.UpdateStatus(status);
            
            Debug.Log($"Matchmaking Status: {status}");
        }
    }
}
