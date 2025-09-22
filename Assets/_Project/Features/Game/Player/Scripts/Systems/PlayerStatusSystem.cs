public class PlayerStatusSystem : IPlayerStatusSystemWriter, IPlayerStatusSystemReader
{
    public void AddPermanentModifier(ulong userId, PlayerStat stat, float value)
    {

    }
    public float GetStatValue(ulong userId, PlayerStat stat)
    {
        return 0;
    }
}