using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class WeightedItem
{
    [Tooltip("The exact 'itemName' from the ItemData ScriptableObject.")]
    public string itemName;
    [Tooltip("The spawning weight. Higher values are more common.")]
    [Min(0f)]
    public float weight;

    // This field is populated at runtime after validation.
    [System.NonSerialized]
    public ItemData itemData;
}

[CreateAssetMenu(fileName = "RandomItemPlacementStrategy", menuName = "Typing Survivor/Item Placement/Random Strategy")]
public class RandomItemPlacementStrategy : ScriptableObject, IItemPlacementStrategy
{
    [Tooltip("The list of items that can be spawned by this strategy and their weights.")]
    [SerializeField] private List<WeightedItem> _spawnableItems;
    
    [Tooltip("The chance (0.0 to 1.0) that an item will be placed on any given empty tile.")]
    [Range(0, 1)]
    [SerializeField] private float _placementChancePerTile = 0.02f;

    private List<WeightedItem> _validatedSpawnableItems;
    private float _totalWeight;
    private bool _isInitialized = false;

    public void Initialize(ItemRegistry itemRegistry)
    {
        _validatedSpawnableItems = new List<WeightedItem>();
        foreach (var weightedItem in _spawnableItems)
        {
            var foundItemData = itemRegistry.AllItems.FirstOrDefault(item => item != null && item.itemName == weightedItem.itemName);
            if (foundItemData != null)
            {
                weightedItem.itemData = foundItemData;
                _validatedSpawnableItems.Add(weightedItem);
            }
            else
            {
                Debug.LogError($"[ItemPlacementStrategy] Item with name '{weightedItem.itemName}' not found in ItemRegistry! It will not be spawned.");
            }
        }
        _totalWeight = _validatedSpawnableItems.Sum(item => item.weight);
        _isInitialized = true;
    }

    public List<TileData> PlaceItems(
        List<TileData> areaBlockTiles, 
        ItemRegistry itemRegistry, 
        System.Random prng, 
        Dictionary<TileBase, int> tileIdMap,
        Vector2Int worldOffset)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[ItemPlacementStrategy] is not initialized. Call Initialize() before use.");
            return new List<TileData>();
        }

        var itemTiles = new List<TileData>();
        if (_validatedSpawnableItems == null || !_validatedSpawnableItems.Any()) return itemTiles;

        var occupiedPositions = new HashSet<Vector3Int>(areaBlockTiles.Select(t => t.Position));

        int minX = occupiedPositions.Any() ? occupiedPositions.Min(p => p.x) : worldOffset.x;
        int maxX = occupiedPositions.Any() ? occupiedPositions.Max(p => p.x) : worldOffset.x;
        int minY = occupiedPositions.Any() ? occupiedPositions.Min(p => p.y) : worldOffset.y;
        int maxY = occupiedPositions.Any() ? occupiedPositions.Max(p => p.y) : worldOffset.y;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var currentPos = new Vector3Int(x, y, 0);
                if (!occupiedPositions.Contains(currentPos) && prng.NextDouble() < _placementChancePerTile)
                {
                    ItemData randomItem = GetRandomItem();
                    if (randomItem != null && randomItem.itemTile != null && tileIdMap.TryGetValue(randomItem.itemTile, out int tileId))
                    {
                        itemTiles.Add(new TileData { Position = currentPos, TileId = tileId });
                    }
                }
            }
        }

        return itemTiles;
    }

    private ItemData GetRandomItem()
    {
        if (_totalWeight <= 0) return null;

        float randomPoint = (float)(new System.Random().NextDouble() * _totalWeight);

        foreach (var item in _validatedSpawnableItems)
        {
            if (randomPoint < item.weight)
            {
                return item.itemData;
            }
            randomPoint -= item.weight;
        }
        return _validatedSpawnableItems.LastOrDefault()?.itemData;
    }
}