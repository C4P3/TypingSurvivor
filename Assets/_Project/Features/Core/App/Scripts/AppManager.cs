using UnityEngine;

namespace TypingSurvivor.Features.Core.App
{
    /// <summary>
    /// The entry point of the application.
    /// Manages the application lifecycle, persisting across scenes.
    /// </summary>
    public class AppManager : MonoBehaviour
    {
        public static AppManager Instance { get; private set; }

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
            // In the future, this method will be responsible for
            // initializing application-wide services like:
            // - Scene Management
            // - Sound Management
            // - Authentication Service
            // - etc.
        }
    }
}
