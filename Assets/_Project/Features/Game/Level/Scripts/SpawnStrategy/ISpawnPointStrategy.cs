using System.Collections.Generic;
using UnityEngine;

namespace TypingSurvivor.Features.Game.Level
{
    /// <summary>
    /// スポーン地点を決定するための戦略インターフェース
    /// </summary>
    public interface ISpawnPointStrategy
    {
        /// <summary>
        /// 指定された条件に基づいてスポーン地点のリストを生成する
        /// </summary>
        /// <param name="playerCount">プレイヤーの数</param>
        /// <param name="walkableTiles">歩行可能なタイル座標のリスト</param>
        /// <param name="mapBounds">マップの境界</param>
        /// <returns>スポーン地点のグリッド座標リスト</returns>
        List<Vector3Int> GetSpawnPoints(int playerCount, List<Vector3Int> walkableTiles, BoundsInt mapBounds);
    }
}
