
using System.Collections.Generic;
using UnityEngine.Tilemaps;

/// <summary>
/// 地形（ブロックタイル）の生成アルゴリズムをカプセル化するストラテジーインターフェース。
/// </summary>
public interface IMapGenerator
{
    /// <summary>
    /// このジェネレーターが使用する可能性のある全てのタイルアセットのリスト。
    /// LevelManagerが起動時にIDマップを生成するために使用する。
    /// </summary>
    List<TileBase> AllTiles { get; }

    /// <summary>
    /// 指定されたシード値に基づき、地形のタイルデータを生成する。
    /// </summary>
    /// <param name="seed">マップ生成に使用するシード値。</param>
    /// <param name="tileIdMap">TileBaseからint IDへの変換マップ。</param>
    /// <returns>生成された地形（ブロック）のTileDataリスト。</returns>
    List<TileData> Generate(long seed, Dictionary<TileBase, int> tileIdMap);
}