using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

using TypingSurvivor.Features.Game.Level.Data;

namespace TypingSurvivor.Features.Game.Level
{
    public interface ILevelService
    {
        // --- World Generation ---
        void GenerateWorld(MapGenerationRequest request);
        List<Vector3Int> GetSpawnPoints(SpawnArea spawnArea);

        // --- Tile Queries & Manipulation ---
        TileBase GetTile(Vector3Int gridPosition);
        bool IsWalkable(Vector3Int gridPosition);
        bool HasItemTile(Vector3Int gridPosition);
        void RemoveItem(Vector3Int gridPosition);
        void DestroyBlock(ulong clientId, Vector3Int gridPosition);
        void ClearArea(Vector3Int gridPosition, int radius);
    }
}