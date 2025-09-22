public interface IPlayerStatusSystem
{
    void AddPermanentModifier(ulong userId, PlayerStat stat, float value);
    float GetStatValue(ulong userId, PlayerStat stat);
}
