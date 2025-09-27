using System;
using Unity.Netcode;

namespace TypingSurvivor.Features.Game.Gameplay.Data
{
    /// <summary>
    /// NetworkListで使うために、INetworkSerializableとIEquatableを実装する必要がある。
    /// </summary>
    public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
    {
        public ulong ClientId;
        public int Score;
        public float Oxygen;
        public bool IsGameOver;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Score);
            serializer.SerializeValue(ref Oxygen);
            serializer.SerializeValue(ref IsGameOver);
        }

        public bool Equals(PlayerData other)
        {
            return ClientId == other.ClientId 
                && Score == other.Score 
                && Oxygen.Equals(other.Oxygen) 
                && IsGameOver == other.IsGameOver;
        }
    }
}