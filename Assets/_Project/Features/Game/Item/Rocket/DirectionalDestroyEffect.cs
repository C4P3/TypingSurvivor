using UnityEngine;

namespace TypingSurvivor.Features.Game.Items.Effects
{
    /// <summary>
    /// Destroys blocks in a straight line based on the user's last movement direction.
    /// </summary>
    [CreateAssetMenu(menuName = "Items/Effects/DirectionalDestroyEffect")]
    public class DirectionalDestroyEffect : ItemEffect
    {
        [SerializeField]
        [Tooltip("How many blocks to destroy in the specified direction.")]
        private int _distance = 10;

        [SerializeField]
        [Tooltip("The default direction to fire in if the player is stationary.")]
        private Vector3Int _defaultDirection = Vector3Int.up;

        public override void Execute(ItemExecutionContext context)
        {
            var direction = context.UserLastMoveDirection;

            // If the player was standing still, use the default direction.
            if (direction == Vector3Int.zero)
            {
                direction = _defaultDirection;
            }

            var startPosition = context.ItemPosition;

            for (int i = 1; i <= _distance; i++)
            {
                var targetPosition = startPosition + (direction * i);
                // Use DestroyBlockAt for single-block destruction without chain reactions.
                context.LevelService.DestroyBlockAt(context.UserId, targetPosition);
            }
        }
    }
}
