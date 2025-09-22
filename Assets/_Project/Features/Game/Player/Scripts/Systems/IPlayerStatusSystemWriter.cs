public interface IPlayerStatusSystemWriter
{
    void AddPermanentModifier(ulong userId, PlayerStat stat, float value);
}