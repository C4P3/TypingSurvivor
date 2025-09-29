using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "VoronoiGenerator", menuName = "Typing Survivor/Map Generators/Voronoi Generator")]
public class VoronoiGenerator : ScriptableObject, IMapGenerator
{
    [Header("Map Dimensions")]
    [SerializeField] private int _width = 100;
    [SerializeField] private int _height = 100;

    [Header("Tile Settings")]
    [Tooltip("The terrain preset to use. The FIRST block type in the list will be used as the wall tile.")]
    [SerializeField] private TerrainPreset _terrainPreset;

    [Header("Voronoi Settings")]
    [Tooltip("The number of 'nuclei' or 'seed points' to generate the Voronoi diagram.")]
    [SerializeField] private int _numberOfPoints = 20;
    [Tooltip("The thickness of the walls generated at the boundaries.")]
    [SerializeField] private int _wallThickness = 1;

    public List<TileData> Generate(long seed, Vector2Int worldOffset, Dictionary<TileBase, int> tileIdMap, Dictionary<string, TileBase> tileNameToTileMap)
    {
        var blockTiles = new List<TileData>();
        if (_terrainPreset == null || _terrainPreset.blockTypes == null || _terrainPreset.blockTypes.Count == 0) return blockTiles;

        var wallTypeName = _terrainPreset.blockTypes[0].tileName;
        if (!tileNameToTileMap.TryGetValue(wallTypeName, out var wallTileAsset) || !tileIdMap.TryGetValue(wallTileAsset, out var wallTileId))
        {
            Debug.LogError($"[VoronoiGenerator] Wall tile '{wallTypeName}' not found.");
            return blockTiles;
        }

        var prng = new System.Random((int)seed);

        // 1. Place random points
        var points = new List<Vector2Int>();
        for (int i = 0; i < _numberOfPoints; i++)
        {
            points.Add(new Vector2Int(prng.Next(0, _width), prng.Next(0, _height)));
        }

        // 2. Assign each tile to the nearest point and draw walls
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                int nearestPointIndex = -1;
                float minDistance = float.MaxValue;

                for (int i = 0; i < points.Count; i++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), points[i]);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestPointIndex = i;
                    }
                }

                // Check neighbors to find boundaries
                bool isBoundary = false;
                for (int nx = x - _wallThickness; nx <= x + _wallThickness; nx++)
                {
                    for (int ny = y - _wallThickness; ny <= y + _wallThickness; ny++)
                    {
                        if (nx >= 0 && nx < _width && ny >= 0 && ny < _height)
                        {
                            int neighborNearestPointIndex = -1;
                            float neighborMinDistance = float.MaxValue;
                            for (int i = 0; i < points.Count; i++)
                            {
                                float distance = Vector2.Distance(new Vector2(nx, ny), points[i]);
                                if (distance < neighborMinDistance)
                                {
                                    neighborMinDistance = distance;
                                    neighborNearestPointIndex = i;
                                }
                            }
                            if (nearestPointIndex != neighborNearestPointIndex)
                            {
                                isBoundary = true;
                                break;
                            }
                        }
                    }
                    if (isBoundary) break;
                }

                if (isBoundary)
                {
                    var tilePos = new Vector3Int(x - _width / 2 + worldOffset.x, y - _height / 2 + worldOffset.y, 0);
                    blockTiles.Add(new TileData { Position = tilePos, TileId = wallTileId });
                }
            }
        }
        return blockTiles;
    }
}
