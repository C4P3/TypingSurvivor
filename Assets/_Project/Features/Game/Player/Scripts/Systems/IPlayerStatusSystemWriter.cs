public interface IPlayerStatusSystemWriter
{
    void AddPermanentModifier(ulong clientId, PlayerStat stat, float value);
}