using UnityEngine;
using TypingSurvivor.Features.Game.Typing;
using TypingSurvivor.Features.Core.PlayerStatus;
using TypingSurvivor.Settings;
using System.Collections.Generic; // Add this for List<>

namespace TypingSurvivor.Features.Game.Settings
{
    [System.Serializable]
    public class LanguageTableMapping
    {
        public string LanguageCode;
        public TypingConversionTableSO ConversionTable;
    }

    [CreateAssetMenu(fileName = "GameConfig", menuName = "Settings/Game Configuration")]
    public class GameConfig : ScriptableObject
    {
        public GameRuleSettings RuleSettings;
        public PlayerDefaultStats PlayerStats;
        public ItemRegistry ItemRegistry;
        public TextAsset WordListCsv;
        public List<LanguageTableMapping> LanguageTables;
        public GameObject PlayerPrefab;
        
        [Header("Spawn Strategies")]
        public ScriptableObject SinglePlayerSpawnStrategy;
        public ScriptableObject VersusSpawnStrategy;
        // ... 他の全体設定アセット ...
    }
}