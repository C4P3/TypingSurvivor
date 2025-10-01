using UnityEngine;
using TypingSurvivor.Features.Core.PlayerStatus;
using TypingSurvivor.Features.Core.VFX; // VFXとSoundIdを使うために追加
using TypingSurvivor.Features.Core.Audio;

namespace TypingSurvivor.Features.Game.Items.Effects
{
    /// <summary>
    /// Stuns all opponent players for a specified duration by setting their MoveSpeed to zero.
    /// </summary>
    [CreateAssetMenu(menuName = "Items/Effects/StunOpponentEffect")]
    public class StunOpponentEffect : ItemEffect
    {
        [Header("Effect Settings")]
        [SerializeField]
        [Tooltip("Duration of the stun effect in seconds.")]
        private float _duration = 5.0f;

        [Header("Visual & Audio")]
        [SerializeField]
        private VFXId _stunVFX = VFXId.LightningStrike; // Inspectorで雷VFXを指定
        [SerializeField]
        private SoundId _stunSound = SoundId.LightningStrike; // Inspectorで雷SFXを指定

        public override void Execute(ItemExecutionContext context)
        {
            if (context.OpponentNetworkObjects == null || context.OpponentNetworkObjects.Count == 0)
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

            foreach (var opponent in context.OpponentNetworkObjects)
            {
                var opponentId = opponent.Key;
                var opponentObject = opponent.Value;

                // Apply the stun status effect
                context.PlayerStatusSystem.ApplyModifier(opponentId, stunModifier);

                // Play the attached visual effect
                if (_stunVFX != VFXId.None && opponentObject != null)
                {
                    context.EffectManager.PlayAttachedEffect(_stunVFX, opponentObject, _duration);
                }

                // Play the sound effect at the opponent's location
                if (_stunSound != SoundId.None && opponentObject != null)
                {
                    context.SfxManager.PlaySfxAtPoint(_stunSound, opponentObject.transform.position);
                }
            }
        }
    }
}
