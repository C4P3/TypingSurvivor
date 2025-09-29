using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "CellularAutomataGenerator", menuName = "Typing Survivor/Map Generators/Cellular Automata Generator")]
public class CellularAutomataGenerator : ScriptableObject, IMapGenerator
{
    [Header("Map Dimensions")]
    [SerializeField] private int _width = 100;
    [SerializeField] private int _height = 100;

    [Header("Tile Settings")]
    [Tooltip("The terrain preset to use. The FIRST block type in the list will be used as the wall tile.")]
    [SerializeField] private TerrainPreset _terrainPreset;

    [Header("Cellular Automata Settings")]
    [Tooltip("The initial percentage of the map that will be filled with walls.")]
    [Range(0, 1)]
    [SerializeField] private float _initialFillPercentage = 0.45f;
    [Tooltip("How many times to run the simulation step.")]
    [SerializeField] private int _simulationSteps = 5;
    [Tooltip("A tile becomes a wall if it has this many wall neighbors or more.")]
    [SerializeField] private int _birthThreshold = 5;
    [Tooltip("A wall tile becomes a floor tile if it has fewer than this many wall neighbors.")]
    [SerializeField] private int _deathThreshold = 4;

    public List<TileData> Generate(long seed, Vector2Int worldOffset, Dictionary<TileBase, int> tileIdMap, Dictionary<string, TileBase> tileNameToTileMap)
    {
        if (_terrainPreset == null || _terrainPreset.blockTypes == null || _terrainPreset.blockTypes.Count == 0)
        {
            Debug.LogError("[CellularAutomataGenerator] Terrain Preset is not set or is empty.");
            return new List<TileData>();
        }
        
        var wallTypeName = _terrainPreset.blockTypes[0].tileName;
        if (!tileNameToTileMap.TryGetValue(wallTypeName, out var wallTileAsset) || !tileIdMap.TryGetValue(wallTileAsset, out var wallTileId))
        {
            Debug.LogError($"[CellularAutomataGenerator] Wall tile '{wallTypeName}' from preset not found or not registered.");
            return new List<TileData>();
        }

        var prng = new System.Random((int)seed);
        int[,] map = new int[_width, _height];

        // 1. Initialize map with random walls
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                map[x, y] = (prng.NextDouble() < _initialFillPercentage) ? 1 : 0;
            }
        }

        // 2. Run simulation steps
        for (int i = 0; i < _simulationSteps; i++)
        {
            int[,] newMap = new int[_width, _height];
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    int neighbors = CountWallNeighbors(map, x, y);
                    if (map[x, y] == 1) // If it's a wall
                    {
                        newMap[x, y] = (neighbors < _deathThreshold) ? 0 : 1;
                    }
                    else // If it's a floor
                    {
                        newMap[x, y] = (neighbors > _birthThreshold) ? 1 : 0;
                    }
                }
            }
            map = newMap;
        }

        // 3. Convert the final map to TileData
        var blockTiles = new List<TileData>();
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (map[x, y] == 1)
                {
                    var tilePos = new Vector3Int(x - _width / 2 + worldOffset.x, y - _height / 2 + worldOffset.y, 0);
                    blockTiles.Add(new TileData { Position = tilePos, TileId = wallTileId });
                }
            }
        }
        return blockTiles;
    }

    private int CountWallNeighbors(int[,] map, int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighborX = gridX - 1; neighborX <= gridX + 1; neighborX++)
        {
            for (int neighborY = gridY - 1; neighborY <= gridY + 1; neighborY++)
            {
                if (neighborX >= 0 && neighborX < _width && neighborY >= 0 && neighborY < _height)
                {
                    if (neighborX != gridX || neighborY != gridY)
                    {
                        wallCount += map[neighborX, neighborY];
                    }
                }
                else
                {
                    // Consider out-of-bounds as walls to create a border
                    wallCount++;
                }
            }
        }
        return wallCount;
    }
}
