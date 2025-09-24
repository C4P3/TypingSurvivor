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
            // Facadeから移動に必要な情報を取得
            var grid = _facade.Grid;
            _moveDuration = _facade.MoveDuration;
            
            // 移動の開始地点と目標地点を設定
            _startPos = _transform.position;
            _targetPos = grid.GetCellCenterWorld(_facade.NetworkGridPosition.Value);
            
            _elapsedTime = 0f;
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
            _transform.position = _targetPos;
        }
    }
}
