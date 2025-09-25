using UnityEngine;
using TypingSurvivor.Features.Game.Typing;
using TypingSurvivor.Features.Core.PlayerStatus;
using TypingSurvivor.Settings;

namespace TypingSurvivor.Features.Game.Settings
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Settings/Game Configuration")]
    public class GameConfig : ScriptableObject
    {
        public GameRuleSettings RuleSettings;
        public PlayerDefaultStats PlayerStats;
        public ItemRegistry ItemRegistry;
        public TypingConversionTableSO TypingConversionTable;
        // ... 他の全体設定アセット ...
    }
}