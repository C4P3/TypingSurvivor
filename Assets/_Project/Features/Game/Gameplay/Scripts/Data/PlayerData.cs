using System;
using Unity.Netcode;
using UnityEngine;

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
        public Vector3Int GridPosition;
        public int BlocksDestroyed;
        public int TypingMisses;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Score);
            serializer.SerializeValue(ref Oxygen);
            serializer.SerializeValue(ref IsGameOver);
            serializer.SerializeValue(ref GridPosition);
            serializer.SerializeValue(ref BlocksDestroyed);
            serializer.SerializeValue(ref TypingMisses);
        }

        public bool Equals(PlayerData other)
        {
            return ClientId == other.ClientId 
                && Score == other.Score 
                && Oxygen.Equals(other.Oxygen) 
                && IsGameOver == other.IsGameOver
                && GridPosition.Equals(other.GridPosition)
                && BlocksDestroyed == other.BlocksDestroyed
                && TypingMisses == other.TypingMisses;
        }
    }
}