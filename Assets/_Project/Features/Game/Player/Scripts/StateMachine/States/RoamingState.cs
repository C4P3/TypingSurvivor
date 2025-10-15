using UnityEngine;

namespace TypingSurvivor.Features.Game.Player
{
    public class RoamingState : IPlayerState
    {
        private readonly PlayerFacade _facade;

        public RoamingState(PlayerFacade facade)
        {
            _facade = facade;
        }

        public void Enter(PlayerState stateFrom)
        {
            // When entering a roaming/idle state, the player's visual position should always be
            // perfectly centered on their logical grid position. This snap corrects any accumulated
            // interpolation errors or visual offsets from previous states (like Moving or a bugged Typing state).
            if (_facade.Grid != null)
            {
                _facade.transform.position = _facade.Grid.GetCellCenterWorld(_facade.NetworkGridPosition.Value);
            }
        }

        public void Execute()
        {
            // 待機中の処理 (特になし)
        }

        public void Exit(PlayerState stateTo)
        {
            // TODO: アイドリングアニメーションの終了など
        }
        
        public void OnTargetPositionChanged(Vector3Int newValue)
        {
            // Roaming状態では何もしない
        }
    }
}
