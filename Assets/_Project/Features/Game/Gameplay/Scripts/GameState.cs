using System;
using System.Collections.Generic;
enum GamePlayState
{
    Preparing,
    Playing,
    Finished
}

public class GameState
{

    public float GameTimer;
    public GamePlayState CurrentState = GamePlayState.Preparing;
    public Dictionary<ulong, int> PlayerScores;
}