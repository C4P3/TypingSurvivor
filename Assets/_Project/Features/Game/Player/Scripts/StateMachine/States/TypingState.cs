using UnityEngine;
using TypingSurvivor.Features.Game.Typing;

namespace TypingSurvivor.Features.Game.Player
{
    public class TypingState : IPlayerState
    {
        private readonly PlayerFacade _facade;
        private float _timeInState;

        public TypingState(PlayerFacade playerFacade)
        {
            _facade = playerFacade;
        }

        public void Enter(PlayerState stateFrom)
        {
            _timeInState = 0f;
            _facade.TypingService?.StartTyping(); // This also resets stats in TypingManager
        }

        public void Execute()
        {
            _timeInState += Time.deltaTime;
        }

        public void Exit(PlayerState stateTo)
        {
            var typingService = _facade.TypingService;
            if (typingService != null)
            {
                // Report stats regardless of whether typing was successful or cancelled
                _facade.ReportTypingSessionStats(
                    _timeInState,
                    typingService.CorrectCharCount,
                    typingService.TotalKeyPressCount
                );
                
                typingService.StopTyping();
            }
        }

        public void OnTargetPositionChanged()
        {
            // Typing状態では何もしない
        }
    }
}
