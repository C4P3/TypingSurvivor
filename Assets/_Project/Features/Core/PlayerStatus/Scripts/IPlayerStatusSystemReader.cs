using System;

namespace TypingSurvivor.Features.Core.PlayerStatus
{
    public interface IPlayerStatusSystemReader
    {
        event Action<ulong, PlayerStat, float> OnStatChanged;
        float GetStatValue(ulong userId, PlayerStat stat);
    }
}