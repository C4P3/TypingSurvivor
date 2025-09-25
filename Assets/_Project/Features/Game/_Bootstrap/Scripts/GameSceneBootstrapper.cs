using UnityEngine;
using TypingSurvivor.Features.Core.App;
using TypingSurvivor.Features.Game.Typing;
using TypingSurvivor.Features.Game.Settings;

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
        // [SerializeField] private InGameHUDManager _hudManager; // In the future, UI can also be initialized here

        private void Awake()
        {
            var serviceLocator = AppManager.Instance;
            if (serviceLocator == null)
            {
                Debug.LogError("AppManager instance not found! Make sure the AppManager scene is loaded first.");
                return;
            }

            // 1. Initialize Core services with data from GameConfig
            serviceLocator.InitializeCoreServices(_gameConfig.PlayerStats);

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
            serviceLocator.RegisterService<ITypingService>(new TypingManager());

            // --- Register MonoBehaviour Services from the scene ---
            serviceLocator.RegisterService<ILevelService>(_levelManager);
            serviceLocator.RegisterService<IItemService>(_itemService);
            serviceLocator.RegisterService<IGameStateWriter>(_gameManager);
            serviceLocator.RegisterService<IGameStateReader>(_gameState); // GameState also acts as a reader
        }

        private void InjectDependencies(IServiceLocator serviceLocator)
        {
            // --- Inject dependencies into GameManager ---
            // TODO: Select strategy based on game mode
            IGameModeStrategy strategy = new SinglePlayerStrategy();
            _gameManager.Initialize(_gameState, strategy);

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
