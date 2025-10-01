using UnityEngine;
using TypingSurvivor.Features.Game.Level.Tiles;
using System.Linq;
using UnityEngine.Tilemaps;
using TypingSurvivor.Features.Core.VFX;
using TypingSurvivor.Features.Core.Audio;

namespace TypingSurvivor.Features.Game.Items.Effects
{
    /// <summary>
    /// Transforms nearby items around opponents into indestructible blocks.
    /// </summary>
    [CreateAssetMenu(menuName = "Items/Effects/TransformItemToBlockEffect")]
    public class TransformItemToBlockEffect : ItemEffect, IBlockProvider
    {
        [Header("Effect Settings")]
        [SerializeField]
        [Tooltip("The indestructible tile to place (e.g., UnchiTile).")]
        private IndestructibleTile _blockToPlace;

        [SerializeField]
        [Tooltip("The maximum radius around each opponent to search for items.")]
        private int _searchRadius = 20;

        [Header("Visual & Audio")]
        [SerializeField] private VFXId _unchiVFX = VFXId.UnchiVFX;
        [SerializeField] private SoundId _unchiSound = SoundId.UnchiEffect;

        public TileBase GetTile()
        {
            return _blockToPlace;
        }

        public override void Execute(ItemExecutionContext context)
        {
            if (_blockToPlace == null)
            {
                Debug.LogError("BlockToPlace is not set on the TransformItemToBlockEffect asset.");
                return;
            }

            if (context.OpponentClientIds == null || context.OpponentClientIds.Count == 0)
            {
                return;
            }

            // Find opponent positions efficiently with a single loop.
            var opponentPositions = new System.Collections.Generic.List<Vector3Int>();
            foreach (var playerData in context.GameStateReader.PlayerDatas)
            {
                if (context.OpponentClientIds.Contains(playerData.ClientId))
                {
                    opponentPositions.Add(playerData.GridPosition);
                }
            }

            foreach (var opponentPos in opponentPositions)
            {
                Vector3Int? closestItemPosition = null;
                float minDistanceSqr = float.MaxValue;

                // Search in a square radius around the opponent to find the single closest item.
                for (int x = -_searchRadius; x <= _searchRadius; x++)
                {
                    for (int y = -_searchRadius; y <= _searchRadius; y++)
                    {
                        var targetPos = opponentPos + new Vector3Int(x, y, 0);

                        if (context.LevelService.HasItemTile(targetPos))
                        {
                            float distanceSqr = (targetPos - opponentPos).sqrMagnitude;
                            // Use <= to ensure that if multiple items are equidistant, one is still chosen.
                            if (distanceSqr <= minDistanceSqr)
                            {
                                minDistanceSqr = distanceSqr;
                                closestItemPosition = targetPos;
                            }
                        }
                    }
                }

                // If an item was found, transform it.
                if (closestItemPosition.HasValue)
                {
                    var finalTargetPos = closestItemPosition.Value;
                    context.LevelService.RemoveItem(finalTargetPos);
                    context.LevelService.PlaceBlock(finalTargetPos, _blockToPlace);

                    // Play VFX and Sound at the location of the new block
                    Vector3 worldPos = context.LevelService.GetWorldPosition(finalTargetPos);
                    if (_unchiVFX != VFXId.None)
                    {
                        context.EffectManager.PlayEffect(_unchiVFX, worldPos);
                    }
                    if (_unchiSound != SoundId.None)
                    {
                        context.SfxManager.PlaySfxAtPoint(_unchiSound, worldPos);
                    }
                }
            }
        }
    }
}
