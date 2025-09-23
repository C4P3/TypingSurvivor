using System;

public class PlayerStateMachine
{
    private PlayerState currentState;
    private IPlayerState[] states;

    public PlayerState CurrentState()
    {
        return currentState;
    }

    public PlayerStateMachine(IPlayerState[] states)
    {
        this.states = states;
    }

    public void Execute()
    {
        states[Convert.ToInt32(currentState)].Execute();
    }

    public void ChangeState(PlayerState nextState)
    {
        var prevState = currentState;
        states[Convert.ToInt32(currentState)].Exit(nextState);
        currentState = nextState;
        states[Convert.ToInt32(currentState)].Enter(prevState);
    }
}