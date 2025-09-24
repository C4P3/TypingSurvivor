
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public interface IMapGenerator
{
    /// <summary>
    /// このジェネレーターが使用する可能性のあるすべてのタイルアセットのリスト
    /// </summary>
    IEnumerable<TileBase> AllTiles { get; }

    /// <summary>
    /// マップデータを生成し、ブロックとアイテムのタイルリストを返す
    /// </summary>
    /// <param name="seed">マップ生成用のシード値</param>
    /// <param name="tileIdMap">TileBaseとTileIdを紐付ける辞書</param>
    /// <returns>生成されたブロックとアイテムのタイルデータ</returns>
    (List<TileData> blockTiles, List<TileData> itemTiles) Generate(long seed, Dictionary<TileBase, int> tileIdMap);
}