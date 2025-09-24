using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "SimpleTestMapGenerator", menuName = "Typing Survivor/Map Generators/Simple Test Map Generator")]
public class SimpleTestMapGenerator : ScriptableObject, IMapGenerator
{
    [SerializeField] private TileBase _wallTile;
    [SerializeField] private int _mapSize = 10;

    public IEnumerable<TileBase> AllTiles
    {
        get { yield return _wallTile; }
    }

    public (List<TileData> blockTiles, List<TileData> itemTiles) Generate(long seed, Dictionary<TileBase, int> tileIdMap)
    {
        var blockTiles = new List<TileData>();
        if (_wallTile == null || !tileIdMap.TryGetValue(_wallTile, out int wallTileId))
        {
            Debug.LogError("Wall Tileが設定されていないか、IDマップに登録されていません。");
            return (blockTiles, new List<TileData>());
        }

        for (int x = -_mapSize; x <= _mapSize; x++)
        {
            for (int y = -_mapSize; y <= _mapSize; y++)
            {
                if (Mathf.Abs(x) == _mapSize || Mathf.Abs(y) == _mapSize)
                {
                    blockTiles.Add(new TileData { Position = new Vector3Int(x, y, 0), TileId = wallTileId });
                }
            }
        }
        return (blockTiles, new List<TileData>());
    }
}