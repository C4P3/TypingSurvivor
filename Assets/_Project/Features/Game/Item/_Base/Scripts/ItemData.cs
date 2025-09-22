using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public TileBase itemTile;

    // Enumの代わりに効果クラスへの参照を持つ
    [SerializeReference] // これでインスペクターから設定可能に
    public List<IItemEffect> Effects;
}