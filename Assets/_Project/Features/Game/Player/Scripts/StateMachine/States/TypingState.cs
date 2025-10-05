using UnityEngine;
using TypingSurvivor.Features.Game.Typing;
using TypingSurvivor.Features.Core.App; // 新しいTypingManagerのnamespace

namespace TypingSurvivor.Features.Game.Player
{
    public class TypingState : IPlayerState
    {
        private readonly PlayerFacade _facade;

        public TypingState(PlayerFacade playerFacade)
        {
            _facade = playerFacade;
        }

        public void Enter(PlayerState stateFrom)
        {
            // TypingManagerに新しいお題の取得とタイピング開始を要求する
            _facade.TypingService?.StartTyping();
        }

        public void Execute()
        {
            // TypingManagerが入力処理を行うため、ここでは何もしない
        }

        public void Exit(PlayerState stateTo)
        {
            _facade.TypingService?.StopTyping();
        }

        public void OnTargetPositionChanged()
        {
            // Typing状態では何もしない
        }
    }
}
