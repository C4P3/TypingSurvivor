namespace TypingSurvivor.Features.Core.PlayerStatus
{
    public enum ModifierScope
    {
        /// <summary>
        /// The effect lasts for the current game session and is cleared on rematch.
        /// </summary>
        Session,
        /// <summary>
        /// The effect is permanent and persists across game sessions (e.g., for roguelike progression).
        /// </summary>
        Persistent
    }

    public class StatModifier
    {
        public readonly PlayerStat Stat;
        public readonly float Value;
        public readonly ModifierType Type;
        public readonly float Duration;
        public readonly ModifierScope Scope;

        public float EndTime { get; private set; }

        public bool IsPermanentDuration => Duration <= 0f;

        public StatModifier(PlayerStat stat, float value, ModifierType type, float duration = 0f, ModifierScope scope = ModifierScope.Session)
        {
            Stat = stat;
            Value = value;
            Type = type;
            Duration = duration;
            Scope = scope;
        }

        public void SetEndTime(float currentTime)
        {
            if (!IsPermanentDuration)
            {
                EndTime = currentTime + Duration;
            }
        }
    }
}