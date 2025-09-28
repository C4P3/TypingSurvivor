using UnityEngine;
using TypingSurvivor.Features.Core.PlayerStatus;

namespace TypingSurvivor.Features.Game.Items.Effects
{
    /// <summary>
    /// Stuns all opponent players for a specified duration by setting their MoveSpeed to zero.
    /// </summary>
    [CreateAssetMenu(menuName = "Items/Effects/StunOpponentEffect")]
    public class StunOpponentEffect : ItemEffect
    {
        [SerializeField]
        [Tooltip("Duration of the stun effect in seconds.")]
        private float _duration = 5.0f;

        public override void Execute(ItemExecutionContext context)
        {
            if (context.OpponentClientIds == null || context.OpponentClientIds.Count == 0)
            {
                return;
            }

            // A multiplicative modifier with a value of 0 will effectively set the final stat to zero.
            var stunModifier = new StatModifier(
                PlayerStat.MoveSpeed,
                0.0f,
                ModifierType.Multiplicative,
                _duration,
                ModifierScope.Session
            );

            foreach (var opponentId in context.OpponentClientIds)
            {
                context.PlayerStatusSystem.ApplyModifier(opponentId, stunModifier);
            }
        }
    }
}
