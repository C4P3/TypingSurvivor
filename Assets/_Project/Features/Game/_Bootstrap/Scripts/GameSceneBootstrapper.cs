using UnityEngine;
using TypingSurvivor.Features.Core.App;
using TypingSurvivor.Features.Game.Typing;
using TypingSurvivor.Features.Game.Settings;
using TypingSurvivor.Features.Core.PlayerStatus;
using System.Collections.Generic;
using TypingSurvivor.Features.Game.Level;
using TypingSurvivor.Features.Game.Camera;
using TypingSurvivor.Features.UI;

using TypingSurvivor.Features.Core.Audio;
using TypingSurvivor.Features.Core.VFX;

namespace TypingSurvivor.Features.Game.Gameplay
{
    /// <summary>
    /// Initializes all necessary services and dependencies for the game scene in the correct order.
    /// This acts as the composition root for the game scene.
    /// </summary>
    public class GameSceneBootstrapper : MonoBehaviour
    {
        [Header("Configuration Assets")]
        [SerializeField] private GameConfig _gameConfig;

        [Header("Scene Dependencies")]
        [SerializeField] private Grid _grid;
        [SerializeField] private GameState _gameState;
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private LevelManager _levelManager;
        [SerializeField] private ItemService _itemService;
        [SerializeField] private CameraManager _cameraManager;
        [SerializeField] private GameUIManager _gameUIManager;

        private void Awake()
        {
            var serviceLocator = AppManager.Instance;
            if (serviceLocator == null)
            {
                Debug.LogError("AppManager instance not found! Make sure the AppManager scene is loaded first.");
                return;
            }

            // 1. Initialize Game services with data from GameConfig
            serviceLocator.InitializeGameServices(_gameConfig.PlayerStats);

            // 2. Register all services to the service locator
            RegisterServices(serviceLocator);

            // 3. Inject dependencies into the systems that need them
            InjectDependencies(serviceLocator);
        }

        private void RegisterServices(IServiceLocator serviceLocator)
        {
            // --- Register Configuration Assets ---
            serviceLocator.RegisterService(_gameConfig);

            // --- Register Plain C# Services ---
            // TypingManagerはWordProviderに依存するため、ここで生成して注入する
            if (_gameConfig.WordListCsv == null)
            {
                Debug.LogError("WordListCsv is not assigned in GameConfig.");
                // CSVがない場合、機能が停止しないように空のWordProviderを登録
                var emptyTables = new Dictionary<string, TypingConversionTable>();
                var emptyWordProvider = new WordProvider("", emptyTables);
                serviceLocator.RegisterService<ITypingService>(new TypingManager(emptyWordProvider));
            }
            else
            {
                // GameConfigから言語テーブルのリストを辞書に変換
                var conversionTables = new Dictionary<string, TypingConversionTable>();
                foreach (var mapping in _gameConfig.LanguageTables)
                {
                    if (mapping.ConversionTable != null && !string.IsNullOrEmpty(mapping.LanguageCode))
                    {
                        conversionTables[mapping.LanguageCode] = mapping.ConversionTable.Table;
                    }
                }

                var wordProvider = new WordProvider(_gameConfig.WordListCsv.text, conversionTables);
                serviceLocator.RegisterService<ITypingService>(new TypingManager(wordProvider));
            }

            // --- Register MonoBehaviour Services from the scene ---
            serviceLocator.RegisterService(_grid);
            serviceLocator.RegisterService<ILevelService>(_levelManager);
            serviceLocator.RegisterService<IItemService>(_itemService);
            serviceLocator.RegisterService<IGameStateWriter>(_gameManager);
            serviceLocator.RegisterService<IGameStateReader>(_gameState); // GameState also acts as a reader
        }

        private void InjectDependencies(IServiceLocator serviceLocator)
        {
            // --- Pre-flight checks for GameConfig assets ---
            if (_gameConfig.PlayerPrefab == null || _gameConfig.DefaultMapGenerator == null || _gameConfig.SinglePlayerSpawnStrategy == null || _gameConfig.VersusSpawnStrategy == null || _gameConfig.DefaultItemPlacementStrategy == null)
            {
                Debug.LogError("GameConfig is missing one or more required asset references. Please check the GameConfig asset in the inspector.");
                return;
            }

            // --- Inject dependencies into LevelManager ---
            var itemPlacementStrategy = AppManager.Instance.GameMode == Core.App.GameModeType.SinglePlayer
                ? _gameConfig.SinglePlayerItemStrategy
                : _gameConfig.MultiPlayerItemStrategy;
            
            if (itemPlacementStrategy == null)
            {
                Debug.LogError($"ItemPlacementStrategy for GameMode '{AppManager.Instance.GameMode}' is not set in GameConfig.");
            }

            _levelManager.Initialize(
                _gameConfig.DefaultMapGenerator,
                itemPlacementStrategy,
                _gameConfig.ItemRegistry,
                _grid,
                _gameConfig
            );

            // --- Inject dependencies into GameManager ---
            IGameModeStrategy strategy;
            GameModeType gameMode = AppManager.Instance.GameMode;

            if (gameMode == GameModeType.SinglePlayer)
            {
                strategy = new SinglePlayerStrategy();
            }
            else
            {
                strategy = new MultiPlayerStrategy();
            }
            
            _gameManager.Initialize(
                _gameState,
                strategy,
                serviceLocator.GetService<ILevelService>(),
                serviceLocator.GetService<IPlayerStatusSystemReader>(),
                serviceLocator.GetService<IPlayerStatusSystemWriter>(),
                _gameConfig, // Pass the whole config for now, can be refined later
                _grid
                );

            // --- Inject dependencies into CameraManager ---
            if (_cameraManager != null)
            {
                _cameraManager.Initialize(serviceLocator.GetService<IGameStateReader>());
            }

            // --- Inject dependencies into ItemService ---
            _itemService.Initialize(
                serviceLocator.GetService<ILevelService>(),
                serviceLocator.GetService<IGameStateReader>(),
                serviceLocator.GetService<IGameStateWriter>(),
                serviceLocator.GetService<IPlayerStatusSystemWriter>(),
                _gameConfig.ItemRegistry,
                SfxManager.Instance,
                EffectManager.Instance,
                _grid
            );

            // --- Inject dependencies into UI ---
            if (_gameUIManager != null)
            {
                _gameUIManager.Initialize(
                    serviceLocator.GetService<IGameStateReader>(),
                    serviceLocator.GetService<IPlayerStatusSystemReader>(),
                    _gameManager,
                    serviceLocator.GetService<ITypingService>(),
                    _cameraManager
                );
            }
        }
    }
}
