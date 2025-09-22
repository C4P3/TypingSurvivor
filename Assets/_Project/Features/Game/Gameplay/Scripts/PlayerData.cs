using System;
using Unity.Netcode;

public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
{
    public ulong ClientId;
    public int Score;
    public bool IsGameOver;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Score);
        serializer.SerializeValue(ref IsGameOver);
    }
    
    public bool Equals(PlayerData other)
    {
        return ClientId == other.ClientId && Score == other.Score && IsGameOver == other.IsGameOver;
    }
}