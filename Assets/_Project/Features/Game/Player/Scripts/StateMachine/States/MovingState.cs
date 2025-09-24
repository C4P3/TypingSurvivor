using UnityEngine;

namespace TypingSurvivor.Features.Game.Player
{
    public class MovingState : IPlayerState
    {
        private readonly PlayerFacade _facade;
        private readonly Transform _transform;

        // --- 内部状態 ---
        private Vector3 _startPos;
        private Vector3 _targetPos;
        private float _moveDuration;
        private float _elapsedTime;

        public MovingState(PlayerFacade facade, Transform transform)
        {
            _facade = facade;
            _transform = transform;
        }

        public void Enter(PlayerState stateFrom)
        {
            ResetMovement();
        }

        public void Execute()
        {
            _elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsedTime / _moveDuration);
            
            // Lerpでスムーズな移動を表現
            _transform.position = Vector3.Lerp(_startPos, _targetPos, t);
        }

        public void Exit(PlayerState stateTo)
        {
            // 念のため、移動完了時に正確な位置にスナップさせる
            // ただし、次のステートもMovingの場合は、中途半端な位置で止まってしまうのでスナップしない
            if (stateTo != PlayerState.Moving)
            {
                _transform.position = _targetPos;
            }
        }

        public void OnTargetPositionChanged()
        {
            // サーバーから目標座標の変更通知が来たので、移動アニメーションをリセット
            ResetMovement();
        }

        private void ResetMovement()
        {
            var grid = _facade.Grid;
            if (grid == null) return; // Facadeの準備ができていない場合は何もしない

            _moveDuration = _facade.MoveDuration;
            _startPos = _transform.position;
            _targetPos = grid.GetCellCenterWorld(_facade.NetworkGridPosition.Value);
            _elapsedTime = 0f;
        }
    }
}
