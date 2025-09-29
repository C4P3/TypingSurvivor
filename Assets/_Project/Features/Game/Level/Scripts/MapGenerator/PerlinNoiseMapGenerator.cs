using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "PerlinNoiseMapGenerator", menuName = "Typing Survivor/Map Generators/Perlin Noise Map Generator")]
public class PerlinNoiseMapGenerator : ScriptableObject, IMapGenerator
{
    [Header("Map Dimensions")]
    [SerializeField] private int _width = 100;
    [SerializeField] private int _height = 100;

    [Header("Block Generation")]
    [SerializeField] private TerrainPreset _terrainPreset;
    [Tooltip("値が小さいほど大きな塊に、大きいほど小さな塊になります。")]
    [SerializeField] private float _noiseScale = 0.4f;
    [Tooltip("この値よりノイズが大きい場所だけにブロックを生成します。値を上げると空間が増えます。")]
    [Range(0, 1)] [SerializeField] private float _blockThreshold = 0.6f;

    public List<TileData> Generate(long seed, Vector2Int worldOffset, Dictionary<TileBase, int> tileIdMap, Dictionary<string, TileBase> tileNameToTileMap)
    {
        var blockTiles = new List<TileData>();
        if (_terrainPreset == null || _terrainPreset.blockTypes == null || _terrainPreset.blockTypes.Count == 0) return blockTiles;

        var prng = new System.Random((int)seed);
        var noiseOffset = new Vector2(prng.Next(-10000, 10000), prng.Next(-10000, 10000));
        
        // Pre-calculate total weight for weighted random selection.
        float totalWeight = _terrainPreset.blockTypes.Sum(bt => bt.probabilityWeight);
        
        // Add a small epsilon to prevent division by zero or flat noise.
        float safeNoiseScale = _noiseScale > 0 ? _noiseScale : 0.001f;

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var tilePos = new Vector3Int(x - _width / 2 + worldOffset.x, y - _height / 2 + worldOffset.y, 0);

                // 1. Generate a single Perlin noise value.
                float noiseX = (tilePos.x + noiseOffset.x) * safeNoiseScale;
                float noiseY = (tilePos.y + noiseOffset.y) * safeNoiseScale;
                float noiseValue = Mathf.PerlinNoise(noiseX, noiseY);

                // 2. If the noise value is above the threshold, place a wall.
                if (noiseValue > _blockThreshold)
                {
                    // 3. Decide WHICH block to place using weighted random selection.
                    BlockTypeSetting chosenBlockSetting = null;
                    if (totalWeight > 0)
                    {
                        float randomPoint = (float)prng.NextDouble() * totalWeight;
                        foreach (var blockType in _terrainPreset.blockTypes)
                        {
                            if (randomPoint < blockType.probabilityWeight)
                            {
                                chosenBlockSetting = blockType;
                                break;
                            }
                            randomPoint -= blockType.probabilityWeight;
                        }
                        // Fallback
                        if (chosenBlockSetting == null)
                        {
                            chosenBlockSetting = _terrainPreset.blockTypes.LastOrDefault(bt => bt.probabilityWeight > 0) ?? _terrainPreset.blockTypes[0];
                        }
                    }
                    else
                    {
                        chosenBlockSetting = _terrainPreset.blockTypes[0];
                    }
                    
                    if (chosenBlockSetting != null && tileNameToTileMap.TryGetValue(chosenBlockSetting.tileName, out var tileAsset))
                    {
                        if (tileIdMap.TryGetValue(tileAsset, out int tileId))
                        {
                            blockTiles.Add(new TileData { Position = tilePos, TileId = tileId });
                        }
                    }
                }
                // If below the threshold, do nothing, leaving it as an empty cave space.
            }
        }
        return blockTiles;
    }
}