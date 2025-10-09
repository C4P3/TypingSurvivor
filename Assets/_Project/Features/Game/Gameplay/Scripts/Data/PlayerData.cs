using System;
using Unity.Collections;
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
        public FixedString64Bytes PlayerName;
        public float Oxygen;
        public bool IsGameOver;
        public Vector3Int GridPosition;
        public int BlocksDestroyed;
        public int TypingMisses;
        public float TotalTimeTyping;
        public int TotalCharsTyped;
        public int TotalKeyPresses;
        public bool IsDisconnected; // To track if the player has disconnected

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref Oxygen);
            serializer.SerializeValue(ref IsGameOver);
            serializer.SerializeValue(ref GridPosition);
            serializer.SerializeValue(ref BlocksDestroyed);
            serializer.SerializeValue(ref TypingMisses);
            serializer.SerializeValue(ref TotalTimeTyping);
            serializer.SerializeValue(ref TotalCharsTyped);
            serializer.SerializeValue(ref TotalKeyPresses);
            serializer.SerializeValue(ref IsDisconnected);
        }

        public bool Equals(PlayerData other)
        {
            return ClientId == other.ClientId 
                && PlayerName.Equals(other.PlayerName)
                && Oxygen.Equals(other.Oxygen) 
                && IsGameOver == other.IsGameOver
                && GridPosition.Equals(other.GridPosition)
                && BlocksDestroyed == other.BlocksDestroyed
                && TypingMisses == other.TypingMisses
                && TotalTimeTyping.Equals(other.TotalTimeTyping)
                && TotalCharsTyped == other.TotalCharsTyped
                && TotalKeyPresses == other.TotalKeyPresses
                && IsDisconnected == other.IsDisconnected;
        }
    }
}