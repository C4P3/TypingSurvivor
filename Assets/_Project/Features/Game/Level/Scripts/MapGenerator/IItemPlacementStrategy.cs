using System.Collections.Generic;
using UnityEngine.Tilemaps;

/// <summary>
/// アイテムの配置アルゴリズムをカプセル化するストラテジーインターフェース。
/// </summary>
public interface IItemPlacementStrategy
{
    /// <summary>
    /// 指定された地形情報とアイテムリストに基づき、アイテムを配置した結果を返す。
    /// </summary>
    /// <param name="blockTiles">アイテムを配置する対象となる地形のタイルリスト。</param>
    /// <param name="itemRegistry">配置するアイテムの候補が登録されたレジストリ。</param>
    /// <param name="prng">配置に使用する疑似乱数生成器。</param>
    /// <param name="tileIdMap">TileBaseからint IDへの変換マップ。</param>
    /// <returns>配置が決定したアイテムのTileDataリスト。</returns>
    List<TileData> PlaceItems(
        List<TileData> blockTiles, 
        ItemRegistry itemRegistry, 
        System.Random prng, 
        Dictionary<TileBase, int> tileIdMap
    );
}
