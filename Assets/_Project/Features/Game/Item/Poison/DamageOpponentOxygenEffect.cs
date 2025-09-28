using UnityEngine;

namespace TypingSurvivor.Features.Game.Items.Effects
{
    /// <summary>
    /// Damages the oxygen of all opponent players.
    /// </summary>
    [CreateAssetMenu(menuName = "Items/Effects/DamageOpponentOxygenEffect")]
    public class DamageOpponentOxygenEffect : ItemEffect
    {
        [SerializeField]
        [Tooltip("The amount of oxygen to remove from each opponent. Should be a positive value.")]
        private float _damageAmount = 15.0f;

        public override void Execute(ItemExecutionContext context)
        {
            if (context.OpponentClientIds == null || context.OpponentClientIds.Count == 0)
            {
                // No opponents to damage, maybe play a "dud" sound?
                return;
            }

            foreach (var opponentId in context.OpponentClientIds)
            {
                // We pass a negative value to AddOxygen to represent damage.
                context.GameStateWriter.AddOxygen(opponentId, -_damageAmount);
            }
        }
    }
}
