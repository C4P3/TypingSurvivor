using UnityEngine;

/// <summary>
/// 全てのアイテム効果ScriptableObjectが継承する抽象基本クラス。
/// これにより、ItemDataのリストで型安全性を確保しつつ、
/// インスペクターからのアセット設定を可能にする。
/// </summary>
public abstract class ItemEffect : ScriptableObject, IItemEffect
{
    public abstract void Execute(ItemExecutionContext context);
}
