using UnityEngine;

namespace TypingSurvivor.Settings
{
    [System.Serializable]
    public class GameRuleSettings
    {
        [Header("Rematch Settings")]
        [Tooltip("マルチプレイモードでの再選待機時間（秒）")]
        public float RematchTimeoutSeconds = 60.0f;
    }
}