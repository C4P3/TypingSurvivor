using System.Collections.Generic;

namespace TypingSurvivor.Features.Game.Level.Data
{
    /// <summary>
    /// A blueprint object that describes the entire world to be generated for a game session.
    /// The GameManager creates this request, and the LevelManager executes it.
    /// </summary>
    public class MapGenerationRequest
    {
        public long BaseSeed;
        public List<SpawnArea> SpawnAreas = new List<SpawnArea>();
    }
}
