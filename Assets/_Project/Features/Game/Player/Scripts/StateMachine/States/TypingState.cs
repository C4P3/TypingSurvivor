namespace TypingSurvivor.Features.Game.Player
{
    public class TypingState : IPlayerState
    {
        public void Enter(PlayerState stateFrom)
        {
            // TODO: タイピングUIの表示、入力マップの切り替えなど
        }

        public void Execute()
        {
            // タイピング中の処理
        }

        public void Exit(PlayerState stateTo)
        {
            // TODO: タイピングUIの非表示、入力マップの復元など
        }
        
        public void OnTargetPositionChanged()
        {
            // Typing状態では何もしない
        }
    }
}
