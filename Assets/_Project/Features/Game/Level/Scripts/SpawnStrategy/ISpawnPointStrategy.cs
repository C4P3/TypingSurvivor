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
        /// 指定されたスポーンエリアのコンテキストに基づいてスポーン地点のリストを生成する
        /// </summary>
        /// <param name="playerCount">このエリアにスポーンするプレイヤーの数</param>
        /// <param name="areaWalkableTiles">このエリア内の歩行可能なタイル座標のリスト</param>
        /// <param name="areaBounds">このエリアの境界</param>
        /// <param name="worldOffset">このエリアのワールド座標オフセット</param>
        /// <returns>スポーン地点のグリッド座標リスト</returns>
        List<Vector3Int> GetSpawnPoints(int playerCount, List<Vector3Int> areaWalkableTiles, BoundsInt areaBounds, Vector2Int worldOffset);
    }
}
