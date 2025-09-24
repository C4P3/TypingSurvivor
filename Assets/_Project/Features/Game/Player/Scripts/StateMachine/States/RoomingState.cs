namespace TypingSurvivor.Features.Game.Player
{
    public class RoamingState : IPlayerState
    {
        public void Enter(PlayerState stateFrom)
        {
            // TODO: アイドリングアニメーションの開始など
        }

        public void Execute()
        {
            // 待機中の処理 (特になし)
        }

        public void Exit(PlayerState stateTo)
        {
            // TODO: アイドリングアニメーションの終了など
        }
    }
}
