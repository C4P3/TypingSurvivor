using Unity.Netcode;
using UnityEngine;

namespace TypingSurvivor.Features.Core.VFX
{
    /// <summary>
    /// Manages the instantiation and playback of all visual effects (VFX) in the application.
    /// This is a persistent singleton that lives throughout the entire application lifecycle.
    /// </summary>
    public class EffectManager : NetworkBehaviour
    {
        public static EffectManager Instance { get; private set; }

        private VFXRegistry _registry;

        public void Initialize(VFXRegistry registry)
        {
            _registry = registry;
            _registry.Initialize();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// Server-side method to request playing a VFX on all clients.
        /// </summary>
        public void PlayEffect(VFXId id, Vector3 position, float scale = 1.0f)
        {
            if (!IsServer) return;
            PlayEffectClientRpc(id, position, scale);
        }

        [ClientRpc]
        private void PlayEffectClientRpc(VFXId id, Vector3 position, float scale)
        {
            var prefab = _registry.GetPrefab(id);
            if (prefab != null)
            {
                var instance = Instantiate(prefab, position, Quaternion.identity);
                instance.transform.localScale = Vector3.one * scale;
            }
        }
    }
}
