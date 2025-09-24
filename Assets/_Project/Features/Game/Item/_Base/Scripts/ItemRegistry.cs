using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "ItemRegistry", menuName = "Items/Item Registry")]
public class ItemRegistry : ScriptableObject
{
    [SerializeField] private List<ItemData> _itemDataList;
    public IReadOnlyList<ItemData> AllItems => _itemDataList;

    private Dictionary<TileBase, ItemData> _tileToItemData;

    private void OnEnable()
    {
        _tileToItemData = new Dictionary<TileBase, ItemData>();
        foreach (var itemData in _itemDataList)
        {
            if (itemData != null && itemData.itemTile != null && !_tileToItemData.ContainsKey(itemData.itemTile))
            {
                _tileToItemData.Add(itemData.itemTile, itemData);
            }
        }
    }

    public ItemData GetItemData(int id)
    {
        if (id >= 0 && id < _itemDataList.Count)
        {
            return _itemDataList[id];
        }
        return null;
    }

    public ItemData GetItemData(TileBase tile)
    {
        _tileToItemData.TryGetValue(tile, out var itemData);
        return itemData;
    }

    public int GetItemId(ItemData itemData)
    {
        return _itemDataList.IndexOf(itemData);
    }
}