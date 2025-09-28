using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using TypingSurvivor.Features.Game.Items.Effects; // Required for IBlockProvider

// A new interface to identify effects that can provide a block tile.
public interface IBlockProvider
{
    TileBase GetTile();
}

[CreateAssetMenu(fileName = "ItemRegistry", menuName = "Items/Item Registry")]
public class ItemRegistry : ScriptableObject
{
    [SerializeField] private List<ItemData> _itemDataList;
    public IReadOnlyList<ItemData> AllItems => _itemDataList;

    public IEnumerable<TileBase> AllEffectTiles
    {
        get
        {
            if (_itemDataList == null) yield break;
            foreach (var itemData in _itemDataList)
            {
                if (itemData.Effects == null) continue;
                foreach (var effect in itemData.Effects)
                {
                    if (effect is IBlockProvider blockProvider)
                    {
                        yield return blockProvider.GetTile();
                    }
                }
            }
        }
    }

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