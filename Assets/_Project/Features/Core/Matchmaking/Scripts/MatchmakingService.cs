using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

namespace TypingSurvivor.Features.Core.Matchmaking
{
    public class MatchmakingResult
    {
        public string Ip;
        public int Port;
    }

    public class MatchmakingService
    {
        public event Action<MatchmakingResult> OnMatchSuccess;
        public event Action<string> OnMatchFailure;
        public event Action<string> OnStatusUpdated;

        private const int TicketCheckIntervalMs = 3000; // 3 seconds
        private const float MatchmakingTimeoutSeconds = 60f; // Overall timeout

        private string _currentTicketId;
        private bool _isMatchmaking;
        private Task _pollingTask;
        private float _timeStarted;

        public async Task<bool> CreateTicketAsync(string queueName, string roomCode = null)
        {
            if (_isMatchmaking) 
            {
                Debug.LogWarning("Matchmaking is already in progress.");
                return false;
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                OnMatchFailure?.Invoke("Not signed in to UGS.");
                return false;
            }

            OnStatusUpdated?.Invoke("Creating matchmaking ticket...");
            _timeStarted = Time.time;

            try
            {
                var attributes = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(roomCode))
                {
                    attributes["room_code"] = roomCode;
                }

                var options = new CreateTicketOptions(queueName, attributes);
                var players = new List<Player> { new Player(AuthenticationService.Instance.PlayerId) };
                var ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options);

                _currentTicketId = ticketResponse.Id;
                _isMatchmaking = true;

                _pollingTask = PollTicketStatusAsync();
                return true;
            }
            catch (Exception e)
            {
                OnMatchFailure?.Invoke($"Failed to create ticket: {e.Message}");
                _isMatchmaking = false;
                return false;
            }
        }

        private async Task PollTicketStatusAsync()
        {
            while (_isMatchmaking && !string.IsNullOrEmpty(_currentTicketId))
            {
                // Check for overall timeout first
                if (Time.time - _timeStarted > MatchmakingTimeoutSeconds)
                {
                    OnMatchFailure?.Invoke("Matchmaking timed out.");
                    await CancelMatchmaking();
                    return;
                }

                try
                {
                    OnStatusUpdated?.Invoke("Searching for match...");
                    TicketStatusResponse ticketStatus = await MatchmakerService.Instance.GetTicketAsync(_currentTicketId);

                    if (ticketStatus?.Value is MultiplayAssignment assignment)
                    {
                        if (assignment.Status == MultiplayAssignment.StatusOptions.Found)
                        {
                            var result = new MatchmakingResult
                            {
                                Ip = assignment.Ip,
                                Port = assignment.Port.Value,
                            };
                            OnMatchSuccess?.Invoke(result);
                            _isMatchmaking = false;
                            return; // Exit loop on success
                        }

                        if (assignment.Status == MultiplayAssignment.StatusOptions.Timeout || assignment.Status == MultiplayAssignment.StatusOptions.Failed)
                        {
                            OnMatchFailure?.Invoke("Matchmaking timed out or failed.");
                            _isMatchmaking = false;
                            return; // Exit loop on failure
                        }
                    }
                }
                catch (Exception e)
                {
                    OnMatchFailure?.Invoke($"Error checking ticket status: {e.Message}");
                    _isMatchmaking = false;
                    return; // Exit loop on error
                }

                await Task.Delay(TicketCheckIntervalMs);
            }
        }

        public async Task CancelMatchmaking()
        {
            if (!_isMatchmaking || string.IsNullOrEmpty(_currentTicketId)) return;

            var ticketId = _currentTicketId;
            _isMatchmaking = false; // Stop the polling loop
            _currentTicketId = null;

            try
            {
                OnStatusUpdated?.Invoke("Cancelling matchmaking...");
                await MatchmakerService.Instance.DeleteTicketAsync(ticketId);
                // On failure/cancellation, let the controller know so it can close the UI.
                OnMatchFailure?.Invoke("Matchmaking cancelled by user.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to delete ticket: {e.Message}");
                // Still notify of failure so the UI can be closed.
                OnMatchFailure?.Invoke("Failed to cancel matchmaking.");
            }
        }
    }
}