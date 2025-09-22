public interface IPlayerState
{
    void Enter(PlayerState stateFrom);
    void Execute();
    void Exit(PlayerState stateTo);
}