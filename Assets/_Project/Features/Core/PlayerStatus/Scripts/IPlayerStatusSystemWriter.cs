using TypingSurvivor.Features.Core.PlayerStatus;

namespace TypingSurvivor.Features.Core.PlayerStatus
{
    public interface IPlayerStatusSystemWriter
    {
        void ApplyModifier(ulong clientId, StatModifier modifier);
    }
}