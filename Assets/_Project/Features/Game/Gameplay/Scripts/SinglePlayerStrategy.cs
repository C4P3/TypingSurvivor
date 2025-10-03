using TypingSurvivor.Features.Game.Gameplay.Data;

namespace TypingSurvivor.Features.Game.Gameplay
{
    public class SinglePlayerStrategy : IGameModeStrategy
    {
        public int PlayerCount => 1;

        public bool IsGameOver(IGameStateReader gameState)
        {
            // The game ends when the single player is game over.
            if (gameState.PlayerDatas.Count > 0)
            {
                return gameState.PlayerDatas[0].IsGameOver;
            }
            // If no player data exists, the game can't continue.
            return true;
        }
        public GameResult CalculateResult(IGameStateReader gameState)
        {
            // In single player, the game ends when the player runs out of oxygen, so it's always a loss.
            // We return a result indicating no draw and an invalid winner ID to signify no one won.
            return new GameResult
            {
                IsDraw = false,
                WinnerClientId = ulong.MaxValue 
            };
        }
    }
}