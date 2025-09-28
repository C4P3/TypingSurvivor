using UnityEngine;
using TypingSurvivor.Features.Core.PlayerStatus;

namespace TypingSurvivor.Features.Game.Items.Effects
{
    /// <summary>
    /// Grants temporary invincibility by applying a 100% DamageReduction modifier.
    /// </summary>
    [CreateAssetMenu(menuName = "Items/Effects/StarEffect")]
    public class StarEffect : ItemEffect
    {
        [SerializeField]
        [Tooltip("Duration of the invincibility effect in seconds.")]
        private float _duration = 10.0f;

        public override void Execute(ItemExecutionContext context)
        {
            var modifier = new StatModifier(
                PlayerStat.DamageReduction,
                1.0f, // 100% reduction
                ModifierType.Additive, // Additive to ensure it stacks to 1.0
                _duration,
                ModifierScope.Session
            );
            
            context.PlayerStatusSystem.ApplyModifier(context.UserId, modifier);
        }
    }
}
