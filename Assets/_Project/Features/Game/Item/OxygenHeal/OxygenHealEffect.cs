using UnityEngine;

/// <summary>
/// 酸素を回復させる即時効果
/// </summary>
[CreateAssetMenu(menuName = "Items/Effects/OxygenHealEffect")]
public class OxygenHealEffect : ScriptableObject, IItemEffect
{
    [SerializeField] private float _amount;

    public void Execute(ItemExecutionContext context)
    {
        // 必要なサービスは全てコンテキストから取得できる
        context.GameStateWriter.AddOxygen(_amount);
    }
}