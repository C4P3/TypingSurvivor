namespace TypingSurvivor.Core.PlayerStatus
{
    public class StatModifier
    {
        public readonly PlayerStat Stat;
        public readonly float Value;
        public readonly ModifierType Type;
        public readonly float Duration;

        public float EndTime { get; private set; }

        public bool IsPermanent => Duration <= 0f;

        public StatModifier(PlayerStat stat, float value, ModifierType type, float duration = 0f)
        {
            Stat = stat;
            Value = value;
            Type = type;
            Duration = duration;
        }

        public void SetEndTime(float currentTime)
        {
            if (!IsPermanent)
            {
                EndTime = currentTime + Duration;
            }
        }
    }
}
