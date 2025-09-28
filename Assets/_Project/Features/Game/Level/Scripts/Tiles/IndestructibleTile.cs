using UnityEngine;
using UnityEngine.Tilemaps;

namespace TypingSurvivor.Features.Game.Level.Tiles
{
    /// <summary>
    /// A specialized tile that cannot be destroyed by normal means.
    /// The LevelManager checks for this type to prevent destruction.
    /// </summary>
    [CreateAssetMenu(fileName = "NewIndestructibleTile", menuName = "Typing Survivor/Tiles/Indestructible Tile")]
    public class IndestructibleTile : Tile
    {
        // This class is intentionally left blank.
        // Its purpose is to act as a "marker" component that other systems can check for.
    }
}
