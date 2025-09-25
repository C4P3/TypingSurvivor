using UnityEngine;
using TypingSurvivor.Features.Core.Auth;
using Unity.Services.Core;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using TypingSurvivor.Features.Core.PlayerStatus;
using UnityEngine.SceneManagement;

namespace TypingSurvivor.Features.Core.App
{
    /// <summary>
    /// The entry point of the application.
    /// Manages the application lifecycle and provides access to core services.
    /// </summary>
    public class AppManager : MonoBehaviour, IServiceLocator
    {
        public static AppManager Instance { get; private set; }
        public static string GameMode { get; set; } = "SinglePlayer"; // To carry game mode selection across scenes
        
        public IAuthenticationService AuthService { get; private set; }
        public IPlayerStatusSystemReader StatusReader { get; private set; }
        public IPlayerStatusSystemWriter StatusWriter { get; private set; }

        public bool IsCoreServicesInitialized { get; private set; }
        public bool IsGameServicesInitialized { get; private set; }

        private PlayerStatusSystem _statusSystem; // 実体への参照を保持
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public T GetService<T>()
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }
            Debug.LogError($"Service of type {typeof(T)} not found.");
            return default;
        }

        public void RegisterService<T>(T service)
        {
            _services[typeof(T)] = service;
        }

        public event Action OnCoreServicesInitialized;

        private async void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Immediately start loading the main menu
            SceneManager.LoadScene("MainMenu");

            // Asynchronously initialize services in the background
            await InitializeCoreServicesAsync();
        }

        private async Task InitializeCoreServicesAsync()
        {
            await InitializeUgsAsync();
            AuthService = new ClientAuthenticationService();
            
            IsCoreServicesInitialized = true;
            // Notify listeners that core services are ready
            OnCoreServicesInitialized?.Invoke();
        }

        private void Update()
        {
            // サーバーサイドでのみ実行
            _statusSystem?.Update();
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

        public void InitializeGameServices(PlayerDefaultStats playerStats)
        {
            Debug.Assert(playerStats != null, "PlayerStats is not assigned in GameConfig.");
            
            _statusSystem = new PlayerStatusSystem(playerStats);
            StatusReader = _statusSystem;
            StatusWriter = _statusSystem;

            // Register the core services so they can be retrieved via GetService<T>
            RegisterService<IPlayerStatusSystemReader>(_statusSystem);
            RegisterService<IPlayerStatusSystemWriter>(_statusSystem);
            
            IsGameServicesInitialized = true;
        }
    }
}
