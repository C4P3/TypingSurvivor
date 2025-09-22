
using System.Collections.Generic;

public interface IMapGenerator
{
    // マップデータを生成し、ブロックとアイテムのタイルリストを返す
    (List<TileData> blockTiles, List<TileData> itemTiles) Generate(long seed);
}