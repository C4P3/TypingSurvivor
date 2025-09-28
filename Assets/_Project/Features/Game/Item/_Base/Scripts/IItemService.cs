using UnityEngine;

public interface IItemService
{
    /// <summary>
    /// サーバー上でアイテム取得処理を実行する
    /// </summary>
    /// <param name="clientId">アイテムを取得したプレイヤーのID</param>
    /// <param name="itemGridPosition">アイテムが存在したグリッド座標</param>
    /// <param name="lastMoveDirection">アイテムを取得したプレイヤーの最後の移動方向</param>
    void AcquireItem(ulong clientId, Vector3Int itemGridPosition, Vector3Int lastMoveDirection);
}