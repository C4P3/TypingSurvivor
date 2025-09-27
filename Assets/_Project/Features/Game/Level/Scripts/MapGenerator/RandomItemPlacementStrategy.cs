using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "RandomItemPlacementStrategy", menuName = "Typing Survivor/Item Placement/Random Strategy")]
public class RandomItemPlacementStrategy : ScriptableObject, IItemPlacementStrategy
{
    [Tooltip("空きスペース1マスあたりにアイテムが配置される確率")]
    [Range(0, 1)]
    [SerializeField] private float _placementChancePerTile = 0.02f;

    public List<TileData> PlaceItems(
        List<TileData> areaBlockTiles, 
        ItemRegistry itemRegistry, 
        System.Random prng, 
        Dictionary<TileBase, int> tileIdMap,
        Vector2Int worldOffset)
    {
        var itemTiles = new List<TileData>();
        if (itemRegistry == null || !areaBlockTiles.Any()) return itemTiles;

        // ブロックが存在する座標を高速に検索できるようHashSetに格納
        var occupiedPositions = new HashSet<Vector3Int>(areaBlockTiles.Select(t => t.Position));

        // エリアの範囲を推定
        int minX = occupiedPositions.Min(p => p.x);
        int maxX = occupiedPositions.Max(p => p.x);
        int minY = occupiedPositions.Min(p => p.y);
        int maxY = occupiedPositions.Max(p => p.y);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var currentPos = new Vector3Int(x, y, 0);
                // その座標にブロックがなく、かつランダム判定に成功した場合にアイテムを配置
                if (!occupiedPositions.Contains(currentPos) && prng.NextDouble() < _placementChancePerTile)
                {
                    // TODO: ItemRegistryに重み付きランダム抽選のメソッドを実装する
                    // 今は仮でID 0のアイテムを配置する
                    ItemData randomItem = itemRegistry.GetItemData(0); 
                    if (randomItem != null && tileIdMap.TryGetValue(randomItem.itemTile, out int tileId))
                    {
                        itemTiles.Add(new TileData { Position = currentPos, TileId = tileId });
                    }
                }
            }
        }

        return itemTiles;
    }
}
