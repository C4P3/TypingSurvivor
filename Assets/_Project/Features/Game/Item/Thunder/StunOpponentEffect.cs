using UnityEngine;
using TypingSurvivor.Features.Core.PlayerStatus;
using TypingSurvivor.Features.Core.VFX; // VFXとSoundIdを使うために追加
using TypingSurvivor.Features.Core.Audio;

namespace TypingSurvivor.Features.Game.Items.Effects
{
    /// <summary>
    /// Stuns all opponent players for a specified duration by setting their MoveSpeed to zero.
    /// </summary>
    [CreateAssetMenu(menuName = "Items/Effects/StunOpponentEffect")]
    public class StunOpponentEffect : ItemEffect
    {
        [Header("Effect Settings")]
        [SerializeField]
        [Tooltip("Duration of the stun effect in seconds.")]
        private float _duration = 5.0f;

        [Header("Visual & Audio")]
        [SerializeField]
        private VFXId _stunVFX = VFXId.LightningStrike; // Inspectorで雷VFXを指定
        [SerializeField]
        private SoundId _stunSound = SoundId.LightningStrike; // Inspectorで雷SFXを指定

        public override void Execute(ItemExecutionContext context)
        {
            if (context.OpponentClientIds == null || context.OpponentClientIds.Count == 0)
            {
                return;
            }

            // A multiplicative modifier with a value of 0 will effectively set the final stat to zero.
            var stunModifier = new StatModifier(
                PlayerStat.MoveSpeed,
                0.0f,
                ModifierType.Multiplicative,
                _duration,
                ModifierScope.Session
            );

            foreach (var opponentId in context.OpponentClientIds)
            {
                // ステータス効果を適用
                context.PlayerStatusSystem.ApplyModifier(opponentId, stunModifier);

                // --- ここから演出の追加 ---
                // 相手プレイヤーのPlayerDataを探す
                Gameplay.Data.PlayerData? targetData = null;
                foreach(var pData in context.GameStateReader.PlayerDatas)
                {
                    if (pData.ClientId == opponentId)
                    {
                        targetData = pData;
                        break;
                    }
                }

                if (targetData.HasValue)
                {
                    // グリッド座標からワールド座標を取得 (このメソッドはLevelServiceにあると仮定)
                    Vector3 opponentWorldPos = context.LevelService.GetWorldPosition(targetData.Value.GridPosition);
                    
                    // 相手の頭上あたりに座標を調整
                    opponentWorldPos.y += 1.5f;

                    // 相手の位置でVFXとSFXを再生するようサーバーから全クライアントに命令
                    if (_stunVFX != VFXId.None)
                        context.EffectManager.PlayEffect(_stunVFX, opponentWorldPos, 1.0f);

                    if (_stunSound != SoundId.None)
                        context.SfxManager.PlaySfxAtPoint(_stunSound, opponentWorldPos);
                }
            }
        }
    }
}
