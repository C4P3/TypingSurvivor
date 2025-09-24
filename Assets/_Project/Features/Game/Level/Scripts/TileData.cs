using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// タイルの種類と座標をネットワークで同期するためのカスタム構造体。
/// NetworkListで使うために、INetworkSerializableとIEquatableを実装する必要がある。
/// </summary>
public struct TileData : INetworkSerializable, IEquatable<TileData>
{
    public Vector3Int Position;
    public int TileId;

    // --- INetworkSerializableの実装 ---
    // データをネットワーク送信用に変換/復元する方法を定義
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref TileId);
    }

    // --- IEquatable<TileData>の実装 ---
    // 2つのTileDataインスタンスが「等しい」かどうかを判断するルールを定義
    public bool Equals(TileData other)
    {
        // PositionとTileIdの両方が同じであれば、等しいとみなす
        return Position.Equals(other.Position) && TileId == other.TileId;
    }
}

