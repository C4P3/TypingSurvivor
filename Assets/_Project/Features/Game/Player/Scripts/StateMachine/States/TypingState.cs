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
            // 入力マップをタイピング用に切り替える
            _facade.PlayerInput.EnableTypingInput();
            
            // TODO: タイピングUIの表示
        }

        public void Execute()
        {
            // タイピング中の処理
        }

        public void Exit(PlayerState stateTo)
        {
            Debug.Log("Exiting Typing State");
            // 入力マップをゲームプレイ用に戻す
            _facade.PlayerInput.EnableGameplayInput();
            
            // TODO: タイピングUIの非表示
        }
        
        public void OnTargetPositionChanged()
        {
            // Typing状態では何もしない
        }
    }
}
