using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "RandomWalkGenerator", menuName = "Typing Survivor/Map Generators/Random Walk Generator")]
public class RandomWalkGenerator : ScriptableObject, IMapGenerator
{
    [Header("Map Dimensions")]
    [SerializeField] private int _width = 100;
    [SerializeField] private int _height = 100;

    [Header("Tile Settings")]
    [Tooltip("The terrain preset to use. The FIRST block type in the list will be used as the wall tile.")]
    [SerializeField] private TerrainPreset _terrainPreset;

    [Header("Random Walk Settings")]
    [Tooltip("The number of walkers to simulate.")]
    [SerializeField] private int _numberOfWalkers = 10;
    [Tooltip("The number of steps each walker will take.")]
    [SerializeField] private int _walkLength = 200;

    public List<TileData> Generate(long seed, Vector2Int worldOffset, Dictionary<TileBase, int> tileIdMap, Dictionary<string, TileBase> tileNameToTileMap)
    {
        if (_terrainPreset == null || _terrainPreset.blockTypes == null || _terrainPreset.blockTypes.Count == 0) return new List<TileData>();

        var wallTypeName = _terrainPreset.blockTypes[0].tileName;
        if (!tileNameToTileMap.TryGetValue(wallTypeName, out var wallTileAsset) || !tileIdMap.TryGetValue(wallTileAsset, out var wallTileId))
        {
            Debug.LogError($"[RandomWalkGenerator] Wall tile '{wallTypeName}' not found.");
            return new List<TileData>();
        }

        var prng = new System.Random((int)seed);
        var floorPositions = new HashSet<Vector2Int>();

        // Run walkers
        for (int i = 0; i < _numberOfWalkers; i++)
        {
            Vector2Int currentPos = new Vector2Int(_width / 2, _height / 2);
            floorPositions.Add(currentPos);

            for (int j = 0; j < _walkLength; j++)
            {
                // Move in a random direction
                int dir = prng.Next(4);
                switch (dir)
                {
                    case 0: currentPos.x++; break;
                    case 1: currentPos.x--; break;
                    case 2: currentPos.y++; break;
                    case 3: currentPos.y--; break;
                }
                
                // Clamp position to be within map bounds
                currentPos.x = Mathf.Clamp(currentPos.x, 1, _width - 2);
                currentPos.y = Mathf.Clamp(currentPos.y, 1, _height - 2);

                floorPositions.Add(currentPos);
            }
        }

        // Create walls everywhere except where the walkers have been
        var blockTiles = new List<TileData>();
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (!floorPositions.Contains(new Vector2Int(x, y)))
                {
                    var tilePos = new Vector3Int(x - _width / 2 + worldOffset.x, y - _height / 2 + worldOffset.y, 0);
                    blockTiles.Add(new TileData { Position = tilePos, TileId = wallTileId });
                }
            }
        }
        return blockTiles;
    }
}
