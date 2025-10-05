using UnityEngine;
using TypingSurvivor.Features.Core.Auth;
using Unity.Services.Core;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using TypingSurvivor.Features.Core.PlayerStatus;
using TypingSurvivor.Features.Core.Matchmaking;
using TypingSurvivor.Features.Core.CloudSave;
#if UNITY_SERVER
using Unity.Services.Authentication.Server;
#endif
using Unity.Netcode.Transports.UTP;
using TypingSurvivor.Features.Core.Audio;
using TypingSurvivor.Features.Core.VFX;
using Unity.Netcode;
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
        public GameModeType GameMode { get; private set; } = GameModeType.SinglePlayer;

        public void SetGameMode(GameModeType mode)
        {
            GameMode = mode;
        }
        
        public TypingSurvivor.Features.Core.Auth.IAuthenticationService AuthService { get; private set; }
        public ICloudSaveService CloudSaveService { get; private set; }
        public MatchmakingService MatchmakingService { get; private set; }
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

        private async void Start()
        {
            var args = System.Environment.GetCommandLineArgs();
            bool isDedicatedServer = System.Array.Exists(args, arg => arg == "-dedicatedServer");

            // Asynchronously initialize services in the background
            await InitializeCoreServicesAsync(isDedicatedServer);

            if (isDedicatedServer)
            {
                // --- Dedicated Server Path ---
                ushort serverPort = 7777;
                string externalServerIP = "0.0.0.0";
                GameModeType gameMode = GameModeType.MultiPlayer;

                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-port" && i + 1 < args.Length) ushort.TryParse(args[i + 1], out serverPort);
                    else if (args[i] == "-ip" && i + 1 < args.Length) externalServerIP = args[i + 1];
                    else if (args[i] == "-gameMode" && i + 1 < args.Length)
                    {
                        if (Enum.TryParse<GameModeType>(args[i + 1], true, out var parsedMode)) gameMode = parsedMode;
                    }
                }
                
                SetGameMode(gameMode);
                StartDedicatedServer(externalServerIP, serverPort);
                NetworkManager.Singleton.SceneManager.LoadScene("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                // --- Client Path ---
                SceneManager.LoadScene("MainMenu");
            }
        }

    [Header("Core Registries")]
    [SerializeField] private AudioRegistry _audioRegistry;
    [SerializeField] private VFXRegistry _vfxRegistry;

    [Header("Core Managers")]
    [SerializeField] private SfxManager _sfxManager;
    [SerializeField] private MusicManager _musicManager;
    [SerializeField] private EffectManager _effectManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // --- Initialize Core Managers ---
            _audioRegistry.Initialize();
            _sfxManager.Initialize(_audioRegistry);
            _effectManager.Initialize(_vfxRegistry);
            _musicManager.Initialize(_audioRegistry); 
        }

        private async Task InitializeCoreServicesAsync(bool isDedicatedServer)
        {
            await InitializeUgsAsync();
            if (isDedicatedServer)
            {
#if UNITY_SERVER
                await ServerAuthenticationService.Instance.SignInFromServerAsync();
                Debug.Log("Server signed in successfully.");
#endif
            }
            else
            {
                AuthService = new ClientAuthenticationService();
            }

            CloudSaveService = new CloudSaveService();
            RegisterService(CloudSaveService);

            MatchmakingService = new MatchmakingService();
            RegisterService(MatchmakingService);
            
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
        
        private void StartDedicatedServer(string ip, ushort port)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().SetConnectionData(ip, port);
            NetworkManager.Singleton.StartServer();
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

        #region Network Start-up

        public void StartGame(GameModeType gameMode)
        {
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;
            
            MusicManager.Instance.Stop(0f);
            SetGameMode(gameMode);
            
            // For single player or private matches, we start as a host.
            if (NetworkManager.Singleton.StartHost())
            {
                NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
            }
        }

        public void StartClient()
        {
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;
            StartClient("127.0.0.1", 7777, GameModeType.MultiPlayer);
        }

        public void StartClient(string ipAddress, ushort port, GameModeType gameMode)
        {
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;

            MusicManager.Instance.Stop(0f);
            SetGameMode(gameMode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipAddress, port);
            NetworkManager.Singleton.StartClient();
        }

        public void StartServer()
        {
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) return;

            MusicManager.Instance.Stop(0f);
            SetGameMode(GameModeType.MultiPlayer);

            if (NetworkManager.Singleton.StartServer())
            {
                NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
            }
        }

        #endregion
    }
}

