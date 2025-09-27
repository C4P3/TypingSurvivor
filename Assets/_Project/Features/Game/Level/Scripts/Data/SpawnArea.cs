using System.Collections.Generic;
using TypingSurvivor.Features.Game.Level;
using UnityEngine;

namespace TypingSurvivor.Features.Game.Level.Data
{
    /// <summary>
    /// Defines a specific area to be generated within the game world.
    /// Part of the MapGenerationRequest blueprint.
    /// </summary>
    public class SpawnArea
    {
        public List<ulong> PlayerClientIds;
        public Vector2Int WorldOffset;
        public IMapGenerator MapGenerator;
        public ISpawnPointStrategy SpawnPointStrategy;
    }
}
