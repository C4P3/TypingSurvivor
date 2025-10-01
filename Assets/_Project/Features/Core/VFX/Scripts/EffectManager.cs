
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

        #region --- PlayEffect (Fire-and-Forget) ---

        /// <summary>
        /// Server-side method to request playing a simple VFX on all clients.
        /// </summary>
        public void PlayEffect(VFXId id, Vector3 position, float scale = 1.0f)
        {
            if (!IsServer) return;
            PlayEffectClientRpc(id, position, Quaternion.identity, Vector3.one * scale);
        }

        /// <summary>
        /// Server-side method to request playing a VFX with specific rotation and scale on all clients.
        /// </summary>
        public void PlayEffect(VFXId id, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!IsServer) return;
            PlayEffectClientRpc(id, position, rotation, scale);
        }

        [ClientRpc]
        private void PlayEffectClientRpc(VFXId id, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var prefab = _registry.GetPrefab(id);
            if (prefab != null)
            {
                var instance = Instantiate(prefab, position, rotation);
                instance.transform.localScale = scale;
            }
        }

        #endregion

        #region --- PlayAttachedEffect (Persistent & Attached) ---

        /// <summary>
        /// Server-side method to request playing a VFX attached to a target for a specific duration.
        /// </summary>
        public void PlayAttachedEffect(VFXId id, NetworkObject target, float duration)
        {
            if (!IsServer) return;
            if (target == null) return;

            PlayAttachedEffectClientRpc(id, new NetworkObjectReference(target), duration);
        }

        [ClientRpc]
        private void PlayAttachedEffectClientRpc(VFXId id, NetworkObjectReference targetRef, float duration)
        {
            if (targetRef.TryGet(out var targetObject))
            {
                var prefab = _registry.GetPrefab(id);
                if (prefab != null)
                {
                    var instance = Instantiate(prefab, targetObject.transform.position, targetObject.transform.rotation);
                    instance.transform.SetParent(targetObject.transform);

                    var autoDestroy = instance.AddComponent<VFXAutoDestroyWithDuration>();
                    autoDestroy.Duration = duration;
                }
            }
        }

        #endregion
    }
}
