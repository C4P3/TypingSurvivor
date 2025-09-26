using UnityEngine;
using TypingSurvivor.Features.Core.App;
using TypingSurvivor.Features.Game.Typing;
using TypingSurvivor.Features.Game.Settings;
using TypingSurvivor.Features.Core.PlayerStatus;
using System.Collections.Generic;
using TypingSurvivor.Features.Game.Level;
using TypingSurvivor.Features.Game.Camera;

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
        [SerializeField] private GameState _gameState;
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private LevelManager _levelManager;
        [SerializeField] private ItemService _itemService;
        [SerializeField] private CameraManager _cameraManager;
        // [SerializeField] private InGameHUDManager _hudManager; // In the future, UI can also be initialized here

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
            serviceLocator.RegisterService<ILevelService>(_levelManager);
            serviceLocator.RegisterService<IItemService>(_itemService);
            serviceLocator.RegisterService<IGameStateWriter>(_gameManager);
            serviceLocator.RegisterService<IGameStateReader>(_gameState); // GameState also acts as a reader
        }

        private void InjectDependencies(IServiceLocator serviceLocator)
        {
            // --- Inject dependencies into GameManager ---
            IGameModeStrategy strategy;
            // AppManagerからゲームモードを取得
            string gameMode = AppManager.GameMode;

            if (gameMode == "SinglePlayer")
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
                serviceLocator.GetService<ILevelService>()
                );

            // --- Inject dependencies into CameraManager ---
            if (_cameraManager != null)
            {
                _cameraManager.Initialize(serviceLocator.GetService<IGameStateReader>());
            }

            // --- Inject dependencies into ItemService ---
            _itemService.Initialize(
                serviceLocator.GetService<ILevelService>(),
                serviceLocator.GetService<IGameStateWriter>(),
                serviceLocator.GetService<IPlayerStatusSystemWriter>()
            );

            // --- Example of injecting dependencies into UI ---
            // if (_hudManager != null)
            // {
            //     _hudManager.Initialize(
            //         serviceLocator.GetService<IGameStateReader>(),
            //         serviceLocator.StatusReader
            //     );
            // }
        }
    }
}
