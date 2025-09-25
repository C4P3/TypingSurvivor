namespace TypingSurvivor.Features.Core.PlayerStatus
{
    public interface IPlayerStatusSystemReader
    {
        float GetStatValue(ulong userId, PlayerStat stat);
    }
}