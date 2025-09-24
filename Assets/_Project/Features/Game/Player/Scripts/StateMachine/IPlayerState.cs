public interface IPlayerState
{
    void Enter(PlayerState stateFrom);
    void Execute();
    void Exit(PlayerState stateTo);
    /// <summary>
    /// ネットワーク同期された目標座標が変更されたときにFacadeから呼び出される
    /// </summary>
    void OnTargetPositionChanged();
}