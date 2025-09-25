using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TypingSurvivor.Features.Game.Level
{
    public interface ILevelService
    {
        TileBase GetTile(Vector3Int gridPosition);
        bool IsWalkable(Vector3Int gridPosition);
        bool HasItemTile(Vector3Int gridPosition);
        void RemoveItem(Vector3Int gridPosition);
        void DestroyBlock(ulong clientId, Vector3Int gridPosition);
        
        List<Vector3Int> GetSpawnPoints(int playerCount, ScriptableObject strategy);
        void ClearArea(Vector3Int gridPosition, int radius);
    }
}