using UnityEngine;

/// <summary>
/// 移動速度を永続的に上昇させるパッシブ効果
/// </summary>
[CreateAssetMenu(menuName = "Items/Effects/MoveSpeedUpEffect")]
public class MoveSpeedUpEffect : ItemEffect
{
    [SerializeField] private float _percentageIncrease;

    public override void Execute(ItemExecutionContext context)
    {
        // プレイヤーのステータスを管理する専門のシステムに依頼する
        context.PlayerStatusSystem.AddPermanentModifier(context.UserId, PlayerStat.MoveSpeed, _percentageIncrease);
    }
}