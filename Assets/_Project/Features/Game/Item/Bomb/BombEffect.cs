using TypingSurvivor.Features.Core.Audio;
using TypingSurvivor.Features.Core.VFX;
using UnityEngine;

/// <summary>
/// 指定範囲のブロックを破壊する効果
/// </summary>
[CreateAssetMenu(menuName = "Items/Effects/BombEffect")]
public class BombEffect : ItemEffect
{
    [SerializeField] private int _radius = 1;
    [SerializeField] private VFXId _explosionVFX = VFXId.BombExplosion;
    [SerializeField] private SoundId _explosionSound = SoundId.BombExplosion;

    public override void Execute(ItemExecutionContext context)
    {
        var levelService = context.LevelService;
        var center = context.ItemPosition;

        // 中心点から半径_radius内の全てのタイルをループ
        for (int x = -_radius; x <= _radius; x++)
        {
            for (int y = -_radius; y <= _radius; y++)
            {
                // 円形の範囲にするために距離をチェック
                if (new Vector2(x, y).magnitude <= _radius)
                {
                    var targetPos = center + new Vector3Int(x, y, 0);
                    // LevelServiceにブロック破壊を依頼
                    levelService.DestroyBlockAt(context.UserId, targetPos);
                }
            }
        }

        // Play explosion effect and sound at the center of the explosion
        var worldCenterPosition = context.WorldPosition;
        float effectScale = _radius * 2.0f;

        if (_explosionVFX != VFXId.None)
        {
            context.EffectManager.PlayEffect(_explosionVFX, worldCenterPosition, effectScale);
        }
        if (_explosionSound != SoundId.None)
        {
            context.SfxManager.PlaySfxAtPoint(_explosionSound, worldCenterPosition);
        }
    }
}
