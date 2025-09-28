using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "SimpleTestMapGenerator", menuName = "Typing Survivor/Map Generators/Simple Test Map Generator")]
public class SimpleTestMapGenerator : ScriptableObject, IMapGenerator
{
    [SerializeField] private TileBase _wallTile;
    [SerializeField] private int _mapSize = 10;

    public List<TileData> Generate(long seed, Vector2Int worldOffset, Dictionary<TileBase, int> tileIdMap, Dictionary<string, TileBase> tileNameToTileMap)
    {
        var blockTiles = new List<TileData>();
        if (_wallTile == null || !tileIdMap.TryGetValue(_wallTile, out int wallTileId))
        {
            Debug.LogError("Wall Tileが設定されていないか、IDマップに登録されていません。");
            return blockTiles;
        }

        for (int x = -_mapSize; x <= _mapSize; x++)
        {
            for (int y = -_mapSize; y <= _mapSize; y++)
            {
                if (Mathf.Abs(x) == _mapSize || Mathf.Abs(y) == _mapSize)
                {
                    blockTiles.Add(new TileData { Position = new Vector3Int(x + worldOffset.x, y + worldOffset.y, 0), TileId = wallTileId });
                }
            }
        }
        return blockTiles;
    }
}