
public interface IPlayerStatusSystemReader
{
    float GetStatValue(PlayerStat stat);

    public float GetStatValue(ulong userId, PlayerStat stat);
}