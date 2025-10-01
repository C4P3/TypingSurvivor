using UnityEngine;
using TypingSurvivor.Features.Core.VFX;
using TypingSurvivor.Features.Core.Audio;

namespace TypingSurvivor.Features.Game.Items.Effects
{
    /// <summary>
    /// Damages the oxygen of all opponent players.
    /// </summary>
    [CreateAssetMenu(menuName = "Items/Effects/DamageOpponentOxygenEffect")]
    public class DamageOpponentOxygenEffect : ItemEffect
    {
        [Header("Effect Settings")]
        [SerializeField]
        [Tooltip("The amount of oxygen to remove from each opponent. Should be a positive value.")]
        private float _damageAmount = 15.0f;

        [Header("Visual & Audio")]
        [SerializeField] private VFXId _poisonVFX = VFXId.PoisonCloudVFX;
        [SerializeField] private SoundId _poisonSound = SoundId.PoisonEffect;
        [SerializeField] private float _effectDuration = 5.0f;

        public override void Execute(ItemExecutionContext context)
        {
            if (context.OpponentNetworkObjects == null || context.OpponentNetworkObjects.Count == 0)
            {
                return;
            }

            foreach (var opponent in context.OpponentNetworkObjects)
            {
                var opponentId = opponent.Key;
                var opponentObject = opponent.Value;

                // Apply the oxygen damage
                context.GameStateWriter.AddOxygen(opponentId, -_damageAmount);

                // Play the attached visual effect
                if (_poisonVFX != VFXId.None && opponentObject != null)
                {
                    context.EffectManager.PlayAttachedEffect(_poisonVFX, opponentObject, _effectDuration);
                }

                // Play the sound effect at the opponent's location
                if (_poisonSound != SoundId.None && opponentObject != null)
                {
                    context.SfxManager.PlaySfxAtPoint(_poisonSound, opponentObject.transform.position);
                }
            }
        }
    }
}
