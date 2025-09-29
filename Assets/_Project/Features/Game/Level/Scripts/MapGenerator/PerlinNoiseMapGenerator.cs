using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Tilemaps;

/// <summary>
/// マップ生成に使用するブロックの種類と設定。
/// </summary>
[System.Serializable]
public class BlockTypeSetting
{
    [Tooltip("The name of the tile to be used, which must correspond to a tile in GameConfig's WorldTiles list.")]
    public string tileName;
    [Tooltip("The probability weight for this block type. Higher values are more likely to be chosen.")]
    public float probabilityWeight = 1.0f;
}

[CreateAssetMenu(fileName = "PerlinNoiseMapGenerator", menuName = "Typing Survivor/Map Generators/Perlin Noise Map Generator")]
public class PerlinNoiseMapGenerator : ScriptableObject, IMapGenerator
{
    [Header("Map Dimensions")]
    [SerializeField] private int _width = 100;
    [SerializeField] private int _height = 100;

    [Header("Block Generation")]
    [SerializeField] private BlockTypeSetting[] _blockTypes;
    [Tooltip("値が小さいほど大きな塊に、大きいほど小さな塊になります。")]
    [SerializeField] private float _noiseScale = 0.4f;
    [Tooltip("この値よりノイズが大きい場所だけにブロックを生成します。値を上げると空間が増えます。")]
    [Range(0, 1)] [SerializeField] private float _blockThreshold = 0.6f;

    public List<TileData> Generate(long seed, Vector2Int worldOffset, Dictionary<TileBase, int> tileIdMap, Dictionary<string, TileBase> tileNameToTileMap)
    {
        var blockTiles = new List<TileData>();
        if (_blockTypes == null || _blockTypes.Length == 0) return blockTiles;

        var prng = new System.Random((int)seed);

        // 1. Generate consistent offsets for each block type's noise map ONCE.
        var noiseOffsets = new Vector2[_blockTypes.Length];
        for (int i = 0; i < _blockTypes.Length; i++)
        {
            noiseOffsets[i] = new Vector2(prng.Next(-10000, 10000), prng.Next(-10000, 10000));
        }

        // Pre-calculate total weight for weighted random selection.
        float totalWeight = _blockTypes.Sum(bt => bt.probabilityWeight);
        
        // Add a small epsilon to prevent division by zero or flat noise.
        float safeNoiseScale = _noiseScale > 0 ? _noiseScale : 0.001f;

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var tilePos = new Vector3Int(x - _width / 2 + worldOffset.x, y - _height / 2 + worldOffset.y, 0);

                // 2. Use the first block type's noise as the primary map to decide IF a block should be placed.
                float noiseX = (tilePos.x + noiseOffsets[0].x) * safeNoiseScale;
                float noiseY = (tilePos.y + noiseOffsets[0].y) * safeNoiseScale;
                float placementNoise = Mathf.PerlinNoise(noiseX, noiseY);

                if (placementNoise > _blockThreshold)
                {
                    // 3. If placing a block, decide WHICH block to place using weighted random selection.
                    BlockTypeSetting chosenBlockSetting = null;
                    if (totalWeight > 0)
                    {
                        float randomPoint = (float)prng.NextDouble() * totalWeight;
                        foreach (var blockType in _blockTypes)
                        {
                            if (randomPoint < blockType.probabilityWeight)
                            {
                                chosenBlockSetting = blockType;
                                break;
                            }
                            randomPoint -= blockType.probabilityWeight;
                        }
                        // Fallback in case of floating point inaccuracies
                        if (chosenBlockSetting == null)
                        {
                            chosenBlockSetting = _blockTypes.LastOrDefault(bt => bt.probabilityWeight > 0) ?? _blockTypes.Last();
                        }
                    }
                    else
                    {
                        // If all weights are zero, just pick the first one.
                        chosenBlockSetting = _blockTypes[0];
                    }
                    
                    if (chosenBlockSetting != null && tileNameToTileMap.TryGetValue(chosenBlockSetting.tileName, out var tileAsset))
                    {
                        if (tileIdMap.TryGetValue(tileAsset, out int tileId))
                        {
                            blockTiles.Add(new TileData { Position = tilePos, TileId = tileId });
                        }
                    }
                    else if (chosenBlockSetting != null)
                    {
                        Debug.LogWarning($"[PerlinNoiseMapGenerator] Tile with name '{chosenBlockSetting.tileName}' not found in the provided tile map.");
                    }
                }
            }
        }
        return blockTiles;
    }
}