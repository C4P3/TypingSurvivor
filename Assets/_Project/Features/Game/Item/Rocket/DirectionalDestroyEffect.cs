using UnityEngine;
using TypingSurvivor.Features.Core.VFX;
using TypingSurvivor.Features.Core.Audio;

namespace TypingSurvivor.Features.Game.Items.Effects
{
    /// <summary>
    /// Destroys blocks in a straight line based on the user's last movement direction.
    /// </summary>
    [CreateAssetMenu(menuName = "Items/Effects/DirectionalDestroyEffect")]
    public class DirectionalDestroyEffect : ItemEffect
    {
        [Header("Effect Settings")]
        [SerializeField]
        [Tooltip("How many blocks to destroy in the specified direction.")]
        private int _distance = 10;

        [SerializeField]
        [Tooltip("The default direction to fire in if the player is stationary.")]
        private Vector3Int _defaultDirection = Vector3Int.up;

        [Header("Visual & Audio")]
        [SerializeField] private VFXId _rocketVFX = VFXId.RocketVFX;
        [SerializeField] private SoundId _rocketSound = SoundId.RocketLaunch;

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

            // --- Play VFX and Sound ---
            if (context.UserNetworkObject == null) return;

            var userPosition = context.UserNetworkObject.transform.position;

            // Play the launch sound at the user's position
            if (_rocketSound != SoundId.None)
            {
                context.SfxManager.PlaySfxAtPoint(_rocketSound, userPosition);
            }

            // Play the directional visual effect
            if (_rocketVFX != VFXId.None)
            {
                // Calculate rotation and position for the VFX
                Quaternion rotation = Quaternion.LookRotation(direction);
                // Center the effect along the path of destruction
                Vector3 effectPosition = userPosition + ((Vector3)direction * _distance / 2.0f);
                // Scale the effect to match the distance
                Vector3 scale = new Vector3(1, 1, _distance);

                context.EffectManager.PlayEffect(_rocketVFX, effectPosition, rotation, scale);
            }
        }
    }
}
