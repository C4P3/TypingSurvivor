using System;

public class PlayerStateMachine
{
    public PlayerState CurrentStateEnum { get; private set; }
    public IPlayerState CurrentIPlayerState { get; private set; }
    
    private readonly IPlayerState[] _states;

    public PlayerStateMachine(IPlayerState[] states)
    {
        _states = states;
        // 初期ステートのインスタンスをセットしておく
        CurrentIPlayerState = _states[0];
    }

    public void Execute()
    {
        CurrentIPlayerState?.Execute();
    }

    public void ChangeState(PlayerState nextState)
    {
        var prevStateEnum = CurrentStateEnum;
        
        CurrentIPlayerState?.Exit(nextState);
        
        CurrentStateEnum = nextState;
        CurrentIPlayerState = _states[Convert.ToInt32(nextState)];
        
        CurrentIPlayerState.Enter(prevStateEnum);
    }
}