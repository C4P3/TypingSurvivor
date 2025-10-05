using System.Threading.Tasks;
using TypingSurvivor.Features.Game.Gameplay.Data;

namespace TypingSurvivor.Features.Game.Rating
{
    public interface IRatingService
    {
        Task<(int, int)> HandleGameFinished(GameResult result);
    }
}
