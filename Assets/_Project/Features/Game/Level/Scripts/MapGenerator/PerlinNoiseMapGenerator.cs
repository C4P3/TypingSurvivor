using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// マップ生成に使用するブロックの種類と設定。
/// </summary>
[System.Serializable]
public class BlockTypeSetting
{
    public string name;
    [Tooltip("LevelManagerのTileIdMapに対応するTile ID")]
    public int tileId;
    [Tooltip("このブロックの出現しやすさ。値が大きいほど優先して選ばれやすくなる。")]
    public float probabilityWeight = 1.0f;
}

[CreateAssetMenu(fileName = "PerlinNoiseMapGenerator", menuName = "Map Generators/Perlin Noise Map Generator")]
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

    [Header("Item Generation")]
    [Tooltip("アイテムを配置する候補地ができる確率")]
    [Range(0, 1)] [SerializeField] private float _itemAreaChance = 0.02f;
    [Tooltip("生成される可能性のあるアイテムのTile IDリスト")]
    [SerializeField] private List<int> _spawnableItemIds;


    public (List<TileData> blockTiles, List<TileData> itemTiles) Generate(long seed)
    {
        var blockTiles = new List<TileData>();
        var itemTiles = new List<TileData>();

        if (_blockTypes == null || _blockTypes.Length == 0)
        {
            Debug.LogError("BlockTypesが設定されていません。");
            return (blockTiles, itemTiles);
        }

        var prng = new System.Random((int)seed);

        // 各ブロックタイプに対応するノイズのオフセットを生成
        var noiseOffsets = new Vector2[_blockTypes.Length];
        for (int i = 0; i < _blockTypes.Length; i++)
        {
            noiseOffsets[i] = new Vector2(prng.Next(-10000, 10000), prng.Next(-10000, 10000));
        }

        // マップ全体をループしてタイルを生成
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var tilePos = new Vector3Int(x, y, 0);

                // --- アイテム生成ロジック ---
                if (_spawnableItemIds.Count > 0 && prng.NextDouble() < _itemAreaChance)
                {
                    // ランダムにアイテムを選ぶ
                    int randomItemId = _spawnableItemIds[prng.Next(0, _spawnableItemIds.Count)];
                    itemTiles.Add(new TileData { Position = tilePos, TileId = randomItemId });
                    continue; // アイテムを置いた場所にはブロックを置かない
                }


                // --- ブロック生成ロジック ---
                BlockTypeSetting chosenBlock = null;
                float maxNoiseValue = -1f;

                for (int i = 0; i < _blockTypes.Length; i++)
                {
                    // パーリンノイズを計算し、ブロックごとの重みを加算
                    float noiseX = (x + noiseOffsets[i].x) * _noiseScale;
                    float noiseY = (y + noiseOffsets[i].y) * _noiseScale;
                    float currentNoise = Mathf.PerlinNoise(noiseX, noiseY) + _blockTypes[i].probabilityWeight;

                    if (currentNoise > maxNoiseValue)
                    {
                        maxNoiseValue = currentNoise;
                        chosenBlock = _blockTypes[i];
                    }
                }

                // 最もノイズ値が高かったブロックを、閾値を超えていれば配置
                if (chosenBlock != null && maxNoiseValue > (_blockThreshold + chosenBlock.probabilityWeight))
                {
                    blockTiles.Add(new TileData { Position = tilePos, TileId = chosenBlock.tileId });
                }
            }
        }

        return (blockTiles, itemTiles);
    }
}