using System;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// レベルの状態を変更したり、情報を問い合わせたりするためのサービスインターフェース。
/// </summary>
public interface ILevelService
{
    event Action<ulong, Vector3Int> OnBlockDestroyed_Server; // どのプレイヤーが、どの座標を破壊したか

    // 指定座標のタイル（ブロックまたはアイテム）を取得する
    TileBase GetTile(Vector3Int gridPosition);
    
    // 指定されたグリッド座標が歩行可能かどうかを判定する。
    bool IsWalkable(Vector3Int gridPosition);

    // 指定されたグリッド座標にアイテムタイルが存在するかどうかを判定する。
    bool HasItemTile(Vector3Int gridPosition);

    // 指定座標のアイテムをマップから取り除く。
    void RemoveItem(Vector3Int gridPosition);

    // 指定座標のブロックを破壊する。破壊した結果(アイテムドロップなど)も責務範囲。
    void DestroyBlock(ulong clientId, Vector3Int gridPosition);
}