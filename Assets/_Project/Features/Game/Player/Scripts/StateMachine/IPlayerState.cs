public interface IPlayerState
{
    void Enter(PlayerState stateFrom);
    void Update();
    void Exit(PlayerState stateTo);
}