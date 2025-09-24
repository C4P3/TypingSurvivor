using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Items/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("基本情報")]
    public string itemName;

    [Header("表現")]
    [Tooltip("UI（インベントリや取得通知）に表示されるアイコン")]
    public Sprite icon;
    [Tooltip("ゲーム世界のタイルマップ上に配置されるタイル")]
    public TileBase itemTile;

    [Header("効果")]
    [Tooltip("このアイテムが持つ効果のリスト")]
    [SerializeField]
    public List<ItemEffect> Effects;
}