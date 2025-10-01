using UnityEngine;
using TypingSurvivor.Features.Core.PlayerStatus;
using TypingSurvivor.Features.Core.VFX;
using TypingSurvivor.Features.Core.Audio;

/// <summary>
/// 移動速度を永続的に上昇させるパッシブ効果
/// </summary>
[CreateAssetMenu(menuName = "Items/Effects/MoveSpeedUpEffect")]
public class MoveSpeedUpEffect : ItemEffect
{
    [Header("Effect Settings")]
    [Tooltip("移動速度の乗率。1.1で10%上昇。")]
    [SerializeField] private float _increaseMultiplier = 1.1f;

    [Header("Visual & Audio")]
    [SerializeField] private VFXId _speedUpVFX = VFXId.SpeedUpVFX;
    [SerializeField] private SoundId _speedUpSound = SoundId.SpeedUpEffect;
    [SerializeField] private float _effectDuration = 2.0f;

    public override void Execute(ItemExecutionContext context)
    {
        var modifier = new StatModifier(
            PlayerStat.MoveSpeed,
            _increaseMultiplier,
            ModifierType.Multiplicative,
            0f, // Duration 0 means permanent for the session
            ModifierScope.Session
        );
        
        // プレイヤーのステータスを管理する専門のシステムに依頼する
        context.PlayerStatusSystem.ApplyModifier(context.UserId, modifier);

        // Play attached effect on the user
        if (_speedUpVFX != VFXId.None && context.UserNetworkObject != null)
        {
            context.EffectManager.PlayAttachedEffect(_speedUpVFX, context.UserNetworkObject, _effectDuration);
        }

        // Play sound effect
        if (_speedUpSound != SoundId.None)
        {
            context.SfxManager.PlaySfxOnAllClients(_speedUpSound);
        }
    }
}