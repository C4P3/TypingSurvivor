using UnityEngine;

namespace TypingSurvivor.Features.Game.Player
{
    public class TypingState : IPlayerState
    {
        private readonly PlayerFacade _facade;

        public TypingState(PlayerFacade facade)
        {
            _facade = facade;
        }

        public void Enter(PlayerState stateFrom)
        {
            Debug.Log("Entering Typing State");
            // TODO: 破壊対象のブロックに応じたお題を取得する
            var challenge = new TypingChallenge { Word = "neko" };
            _facade.TypingManager?.StartTyping(challenge);
        }

        public void Execute()
        {
            // TypingManagerが入力処理を行うため、ここでは何もしない
        }

        public void Exit(PlayerState stateTo)
        {
            Debug.Log("Exiting Typing State");
            _facade.TypingManager?.StopTyping();
        }
        
        public void OnTargetPositionChanged()
        {
            // Typing状態では何もしない
        }
    }
}
