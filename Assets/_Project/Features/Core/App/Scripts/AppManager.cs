using UnityEngine;
using TypingSurvivor.Features.Core.Auth;

namespace TypingSurvivor.Features.Core.App
{
    /// <summary>
    /// The entry point of the application.
    /// Manages the application lifecycle and provides access to core services.
    /// </summary>
    public class AppManager : MonoBehaviour
    {
        public static AppManager Instance { get; private set; }

        public IAuthenticationService AuthService { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeServices();
        }

        private void InitializeServices()
        {
            // Create and hold the instance of the authentication service.
            AuthService = new AuthenticationService();

            // In the future, this method will also be responsible for
            // initializing other application-wide services like:
            // - Scene Management
            // - Sound Management
            // - etc.
        }
    }
}
