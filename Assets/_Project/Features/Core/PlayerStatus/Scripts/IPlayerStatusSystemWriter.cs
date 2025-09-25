using TypingSurvivor.Core.PlayerStatus;

public interface IPlayerStatusSystemWriter
{
    void ApplyModifier(ulong clientId, StatModifier modifier);
}