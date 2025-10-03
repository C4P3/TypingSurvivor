using TypingSurvivor.Features.Core.App;
using TypingSurvivor.Features.Core.Matchmaking;
using TypingSurvivor.Features.UI.Common;
using UnityEngine;
using System.Collections; // For Coroutines

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    public class MatchmakingController : MonoBehaviour
    {
        private MatchmakingService _matchmakingService;
        private UIManager _uiManager;
        private AppManager _appManager;

        [Header("UI Panels")]
        [SerializeField] private MatchmakingWaitController _matchmakingWaitPanel;

        public void Initialize(MatchmakingService matchmakingService, UIManager uiManager, AppManager appManager)
        {
            _matchmakingService = matchmakingService;
            _uiManager = uiManager;
            _appManager = appManager;
            
            _matchmakingWaitPanel.Initialize(this); // Initialize the wait panel
            
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (_matchmakingService == null) return;
            _matchmakingService.OnMatchSuccess += HandleMatchSuccess;
            _matchmakingService.OnMatchFailure += HandleMatchFailure;
            _matchmakingService.OnStatusUpdated += HandleStatusUpdate;
        }

        private void UnsubscribeFromEvents()
        {
            if (_matchmakingService == null) return;
            _matchmakingService.OnMatchSuccess -= HandleMatchSuccess;
            _matchmakingService.OnMatchFailure -= HandleMatchFailure;
            _matchmakingService.OnStatusUpdated -= HandleStatusUpdate;
        }

        public void StartPublicMatchmaking(string queueName)
        {
            // The UIFlowCoordinator is responsible for pushing the panel, 
            // so we just update the status of the (now visible) panel.
            _matchmakingWaitPanel.UpdateStatus("Searching for a match...");
            _matchmakingService.CreateTicketAsync(queueName);
        }

        public void StartPrivateMatchmaking(string roomCode)
        {
            if (string.IsNullOrEmpty(roomCode)) 
            {
                HandleMatchFailure("Room code cannot be empty.");
                return;
            }
            _matchmakingWaitPanel.UpdateStatus("Joining private room...");
            // Note: This logic might be incorrect for private matches as per design docs (Relay vs Matchmaker).
            // Assuming ticket-based system for now for simplicity.
            _matchmakingService.CreateTicketAsync("PrivateQueue", roomCode);
        }

        public void Cancel()
        {
            // This will trigger the OnMatchFailure event with a "Cancelled" reason.
            _matchmakingService.CancelMatchmaking();
        }

        private void HandleMatchSuccess(MatchmakingResult result)
        {
            _matchmakingWaitPanel.UpdateStatus("Match Found! Connecting...");
            
            // Short delay for user to read the message, then connect.
            // The panel will be hidden automatically by the scene change.
            StartCoroutine(ConnectAfterDelay(result, 1.5f));
        }

        private IEnumerator ConnectAfterDelay(MatchmakingResult result, float delay)
        {            
            yield return new WaitForSeconds(delay);
            Debug.Log($"Match found! Connecting to {result.Ip}:{result.Port}");
            _appManager.StartClient(result.Ip, (ushort)result.Port);
        }

        private void HandleMatchFailure(string reason)
        {
            _matchmakingWaitPanel.UpdateStatus($"Failed: {reason}");
            
            // After showing the error for a moment, close the panel.
            StartCoroutine(ClosePanelAfterDelay(2.0f));
        }

        private IEnumerator ClosePanelAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _uiManager.PopPanel();
        }

        private void HandleStatusUpdate(string status)
        {            
            _matchmakingWaitPanel.UpdateStatus(status);
            Debug.Log($"Matchmaking Status: {status}");
        }
    }
}