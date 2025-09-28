using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

/// <summary>
/// 地形（ブロックタイル）の生成アルゴリズムをカプセル化するストラテジーインターフェース。
/// </summary>
public interface IMapGenerator
{
    /// <summary>
    /// 指定されたシード値に基づき、地形のタイルデータを生成する。
    /// </summary>
    /// <param name="seed">マップ生成に使用するシード値。</param>
    /// <param name="worldOffset">このマップ領域を生成するワールド座標のオフセット。</param>
    /// <param name="tileIdMap">TileBaseからint IDへの変換マップ。</param>
    /// <param name="tileNameToTileMap">このジェネレーターが使用するタイル名とTileBaseアセットの対応辞書。</param>
    /// <returns>生成された地形（ブロック）のTileDataリスト。</returns>
    List<TileData> Generate(long seed, Vector2Int worldOffset, Dictionary<TileBase, int> tileIdMap, Dictionary<string, TileBase> tileNameToTileMap);
}