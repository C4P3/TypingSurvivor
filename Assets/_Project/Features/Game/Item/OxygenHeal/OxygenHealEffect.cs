using UnityEngine;
using TypingSurvivor.Features.Core.VFX;
using TypingSurvivor.Features.Core.Audio;

/// <summary>
/// 酸素を回復させる即時効果
/// </summary>
[CreateAssetMenu(menuName = "Items/Effects/OxygenHealEffect")]
public class OxygenHealEffect : ItemEffect
{
    [Header("Effect Settings")]
    [SerializeField] private float _amount;

    [Header("Visual & Audio")]
    [SerializeField] private VFXId _healVFX = VFXId.HealVFX;
    [SerializeField] private SoundId _healSound = SoundId.HealEffect;
    [SerializeField] private float _effectDuration = 2.0f;

    public override void Execute(ItemExecutionContext context)
    {
        // 必要なサービスは全てコンテキストから取得できる
        context.GameStateWriter.AddOxygen(context.UserId, _amount);

        // Play attached effect on the user
        if (_healVFX != VFXId.None && context.UserNetworkObject != null)
        {
            context.EffectManager.PlayAttachedEffect(_healVFX, context.UserNetworkObject, _effectDuration);
        }

        // Play sound effect
        if (_healSound != SoundId.None)
        {
            context.SfxManager.PlaySfxOnAllClients(_healSound);
        }
    }
}