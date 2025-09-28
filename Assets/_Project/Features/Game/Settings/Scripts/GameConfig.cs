using UnityEngine;
using TypingSurvivor.Features.Game.Typing;
using TypingSurvivor.Features.Core.PlayerStatus;
using TypingSurvivor.Settings;
using System.Collections.Generic;
using TypingSurvivor.Features.Game.Level; // Add this for List<>

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
        [SerializeField] private ScriptableObject _singlePlayerSpawnStrategy;
        public ISpawnPointStrategy SinglePlayerSpawnStrategy => _singlePlayerSpawnStrategy as ISpawnPointStrategy;
        
        [SerializeField] private ScriptableObject _versusSpawnStrategy;
        public ISpawnPointStrategy VersusSpawnStrategy => _versusSpawnStrategy as ISpawnPointStrategy;

        [Header("Item Placement Strategies")]
        [SerializeField] private ScriptableObject _defaultItemPlacementStrategy;
        public IItemPlacementStrategy DefaultItemPlacementStrategy => _defaultItemPlacementStrategy as IItemPlacementStrategy;
        [SerializeField] private ScriptableObject _singlePlayerItemStrategy;
        public IItemPlacementStrategy SinglePlayerItemStrategy => _singlePlayerItemStrategy as IItemPlacementStrategy;

        [SerializeField] private ScriptableObject _multiPlayerItemStrategy;
        public IItemPlacementStrategy MultiPlayerItemStrategy => _multiPlayerItemStrategy as IItemPlacementStrategy;

        [Header("Map Generators")]
        [SerializeField] private ScriptableObject _defaultMapGenerator;
        public IMapGenerator DefaultMapGenerator => _defaultMapGenerator as IMapGenerator;
        // ... 他の全体設定アセット ...
    }
}