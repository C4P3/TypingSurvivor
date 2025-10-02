using TypingSurvivor.Features.Core.App;
using TypingSurvivor.Features.Core.Matchmaking;
using TypingSurvivor.Features.UI.Common;
using UnityEngine;

namespace TypingSurvivor.Features.UI.Screens.MainMenu
{
    public class MatchmakingController : MonoBehaviour
    {
        private MatchmakingService _matchmakingService;
        private UIManager _uiManager;
        private AppManager _appManager;

        [Header("UI Panels")]
        [SerializeField] private ScreenBase _matchmakingPanel;
        [SerializeField] private ScreenBase _roomCodePanel; // For private matches

        public void Initialize(MatchmakingService matchmakingService, UIManager uiManager, AppManager appManager)
        {
            _matchmakingService = matchmakingService;
            _uiManager = uiManager;
            _appManager = appManager;
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
            _uiManager.PushPanel(_matchmakingPanel);
            _matchmakingService.CreateTicketAsync(queueName);
        }

        public void StartPrivateMatch(string roomCode)
        {
            if (string.IsNullOrEmpty(roomCode)) 
            {
                HandleMatchFailure("Room code cannot be empty.");
                return;
            }
            _uiManager.PushPanel(_matchmakingPanel);
            _matchmakingService.CreateTicketAsync("PrivateQueue", roomCode);
        }

        public void Cancel()
        {
            _matchmakingService.CancelMatchmaking();
            _uiManager.PopPanel();
        }

        private void HandleMatchSuccess(MatchmakingResult result)
        {
            // TODO: Update UI to show "Match Found!"
            Debug.Log($"Match found! Connecting to {result.Ip}:{result.Port}");
            _appManager.StartClient(result.Ip, (ushort)result.Port);
        }

        private void HandleMatchFailure(string reason)
        {
            // TODO: Update UI to show the error message
            Debug.LogError($"Matchmaking Failed: {reason}");
            _uiManager.PopPanel();
        }

        private void HandleStatusUpdate(string status)
        {
            // TODO: Update the text on the _matchmakingPanel
            Debug.Log($"Matchmaking Status: {status}");
        }
    }
}
