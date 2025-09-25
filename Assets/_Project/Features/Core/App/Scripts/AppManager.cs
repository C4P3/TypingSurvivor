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
        
        public IAuthenticationService AuthService { get; private set; }
        public IPlayerStatusSystemReader StatusReader { get; private set; }
        public IPlayerStatusSystemWriter StatusWriter { get; private set; }

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

            // Always load the MainMenu scene after initialization
            SceneManager.LoadScene("Game");
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

        public void InitializeCoreServices(PlayerDefaultStats playerStats)
        {
            Debug.Assert(playerStats != null, "PlayerStats is not assigned in GameConfig.");

            // --- Core Services ---
            AuthService = new ClientAuthenticationService();
            
            _statusSystem = new PlayerStatusSystem(playerStats);
            StatusReader = _statusSystem;
            StatusWriter = _statusSystem;

            // Register the core services so they can be retrieved via GetService<T>
            RegisterService<IPlayerStatusSystemReader>(_statusSystem);
            RegisterService<IPlayerStatusSystemWriter>(_statusSystem);
        }
    }
}
