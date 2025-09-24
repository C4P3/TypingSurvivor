using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SimpleTestMapGenerator", menuName = "Map Generators/Simple Test Map Generator")]
public class SimpleTestMapGenerator : ScriptableObject, IMapGenerator
{
    public (List<TileData> blockTiles, List<TileData> itemTiles) Generate(long seed)
    {
        var blockTiles = new List<TileData>();
        int mapSize = 4; // 10x10の壁

        for (int x = -mapSize; x <= mapSize; x++)
        {
            for (int y = -mapSize; y <= mapSize; y++)
            {
                // 外周のみ壁を配置
                if (Mathf.Abs(x) == mapSize || Mathf.Abs(y) == mapSize)
                {
                // LevelManagerのTileIdMap[0]のタイルを使う
                blockTiles.Add(new TileData { Position = new Vector3Int(x, y, 0), TileId = 0 });
                }
            }
        }
        return (blockTiles, new List<TileData>()); // アイテムは無し
    }
}