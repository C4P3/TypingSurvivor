using UnityEngine;
using TypingSurvivor.Features.Core.Auth;
using Unity.Services.Core;
using System.Threading.Tasks;

using TypingSurvivor.Features.Game.Typing;

namespace TypingSurvivor.Features.Core.App
{
    /// <summary>
    /// The entry point of the application.
    /// Manages the application lifecycle and provides access to core services.
    /// </summary>
    public class AppManager : MonoBehaviour
    {
        public static AppManager Instance { get; private set; }
        [SerializeField] public GameConfig gameConfig;
        public IAuthenticationService AuthService { get; private set; }
        public IPlayerStatusSystemReader StatusReader { get; private set; }
        public IPlayerStatusSystemWriter StatusWriter { get; private set; }
        public ILevelService LevelService { get; private set; }
        public IItemService ItemService { get; private set; }
        public ITypingService TypingService { get; private set; }

        private async void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            await InitializeUgsAsync();

            // TODO: サーバー/クライアントの起動フローを管理するクラスに移動すべき
            InitializeServices();
        }

        private async Task InitializeUgsAsync()
        {
            try
            {
                await UnityServices.InitializeAsync();
                Debug.Log("Unity Gaming Services initialized successfully.");
            }
            catch (ServicesInitializationException e)
            {
                Debug.LogError($"Failed to initialize Unity Gaming Services: {e.Message}");
            }
        }

        private void InitializeServices()
        {
            // --- Plain C# Services ---
            AuthService = new ClientAuthenticationService();
            TypingService = new TypingManager();
            
            var statusSystem = new PlayerStatusSystem();
            StatusReader = statusSystem;
            StatusWriter = statusSystem;

            // --- MonoBehaviour Services (Scene-dependent) ---
            // TODO: シーンロードのたびに再検索が必要になる可能性がある
            LevelService = FindObjectOfType<LevelManager>();
            ItemService = FindObjectOfType<ItemService>();

            // In the future, this method will also be responsible for
            // initializing other application-wide services like:
            // - Scene Management
            // - Sound Management
            // - etc.
        }
    }
}
