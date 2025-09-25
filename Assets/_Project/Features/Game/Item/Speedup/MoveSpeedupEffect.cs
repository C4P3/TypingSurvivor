using UnityEngine;
using TypingSurvivor.Core.PlayerStatus;

/// <summary>
/// 移動速度を永続的に上昇させるパッシブ効果
/// </summary>
[CreateAssetMenu(menuName = "Items/Effects/MoveSpeedUpEffect")]
public class MoveSpeedUpEffect : ItemEffect
{
    [Tooltip("移動速度の乗率。1.1で10%上昇。")]
    [SerializeField] private float _increaseMultiplier = 1.1f;

    public override void Execute(ItemExecutionContext context)
    {
        var modifier = new StatModifier(
            PlayerStat.MoveSpeed,
            _increaseMultiplier,
            ModifierType.Multiplicative
        );
        
        // プレイヤーのステータスを管理する専門のシステムに依頼する
        context.PlayerStatusSystem.ApplyModifier(context.UserId, modifier);
    }
}