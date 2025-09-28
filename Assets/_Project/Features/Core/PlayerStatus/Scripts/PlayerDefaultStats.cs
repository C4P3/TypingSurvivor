using UnityEngine;

namespace TypingSurvivor.Features.Core.PlayerStatus
{
    [CreateAssetMenu(fileName = "PlayerDefaultStats", menuName = "Settings/Player Default Stats")]
    public class PlayerDefaultStats : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Base movement speed in tiles per second.")]
        public float MoveSpeed = 4.0f;

        [Header("Resources")]
        [Tooltip("Maximum oxygen level.")]
        public float MaxOxygen = 100f;

        [Header("Interaction")]
        [Tooltip("Base range for detecting items or enemies.")]
        public float RadarRange = 5f;

        [Header("Combat")]
        [Tooltip("Base damage reduction percentage (0.0 = 0%, 1.0 = 100%).")]
        [Range(0.0f, 1.0f)]
        public float DamageReduction = 0.0f;

        public float GetBaseStatValue(PlayerStat stat)
        {
            switch (stat)
            {
                case PlayerStat.MoveSpeed: return MoveSpeed;
                case PlayerStat.MaxOxygen: return MaxOxygen;
                case PlayerStat.RadarRange: return RadarRange;
                case PlayerStat.DamageReduction: return DamageReduction;
                default:
                    Debug.LogWarning($"Base value for stat '{stat}' not defined in PlayerDefaultStats.");
                    return 1.0f;
            }
        }
    }
}
