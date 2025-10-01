using UnityEngine;
using TypingSurvivor.Features.Core.PlayerStatus;
using TypingSurvivor.Features.Core.VFX;
using TypingSurvivor.Features.Core.Audio;

namespace TypingSurvivor.Features.Game.Items.Effects
{
    /// <summary>
    /// Grants temporary invincibility by applying a 100% DamageReduction modifier.
    /// </summary>
    [CreateAssetMenu(menuName = "Items/Effects/StarEffect")]
    public class StarEffect : ItemEffect
    {
        [Header("Effect Settings")]
        [SerializeField]
        [Tooltip("Duration of the invincibility effect in seconds.")]
        private float _duration = 10.0f;

        [Header("Visual & Audio")]
        [SerializeField] private VFXId _starVFX = VFXId.StarActivateVFX;
        [SerializeField] private SoundId _starSound = SoundId.StarActivate;

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

            // Play attached effect on the user
            if (_starVFX != VFXId.None && context.UserNetworkObject != null)
            {
                context.EffectManager.PlayAttachedEffect(_starVFX, context.UserNetworkObject, _duration);
            }

            // Play sound effect
            if (_starSound != SoundId.None)
            {
                // Play for all clients to hear, as it's a significant event
                context.SfxManager.PlaySfxOnAllClients(_starSound);
            }
        }
    }
}
