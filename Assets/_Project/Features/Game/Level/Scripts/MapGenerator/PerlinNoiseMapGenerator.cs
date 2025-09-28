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
        var prng = new System.Random((int)seed);

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var tilePos = new Vector3Int(x - _width / 2 + worldOffset.x, y - _height / 2 + worldOffset.y, 0);

                // --- ブロック生成ロジック ---
                BlockTypeSetting chosenBlockSetting = null;
                float maxNoiseValue = -1f;

                foreach (var blockSetting in _blockTypes)
                {
                    float noiseX = (tilePos.x + prng.Next(-1000, 1000)) * _noiseScale;
                    float noiseY = (tilePos.y + prng.Next(-1000, 1000)) * _noiseScale;
                    float currentNoise = Mathf.PerlinNoise(noiseX, noiseY) * blockSetting.probabilityWeight;

                    if (currentNoise > maxNoiseValue)
                    {
                        maxNoiseValue = currentNoise;
                        chosenBlockSetting = blockSetting;
                    }
                }

                if (chosenBlockSetting != null && maxNoiseValue > _blockThreshold)
                {
                    if (tileNameToTileMap.TryGetValue(chosenBlockSetting.tileName, out var tileAsset))
                    {
                        if (tileIdMap.TryGetValue(tileAsset, out int tileId))
                        {
                            blockTiles.Add(new TileData { Position = tilePos, TileId = tileId });
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[PerlinNoiseMapGenerator] Tile with name '{chosenBlockSetting.tileName}' not found in the provided tile map.");
                    }
                }
            }
        }
        return blockTiles;
    }
}