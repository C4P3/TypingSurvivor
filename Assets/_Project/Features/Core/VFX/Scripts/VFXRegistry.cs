using System.Collections.Generic;
using UnityEngine;

namespace TypingSurvivor.Features.Core.VFX
{
    public enum VFXId
    {
        None,
        // Items
        BombExplosion = 1,
        LightningStrike = 2,
        // Other
        BlockDestroy = 100,
    }

    [System.Serializable]
    public class VFXEntry
    {
        public VFXId Id;
        public GameObject Prefab;
    }

    [CreateAssetMenu(fileName = "VFXRegistry", menuName = "Typing Survivor/VFX/VFX Registry")]
    public class VFXRegistry : ScriptableObject
    {
        [SerializeField] private List<VFXEntry> _vfx;

        private Dictionary<VFXId, GameObject> _vfxDictionary;

        public void Initialize()
        {
            _vfxDictionary = new Dictionary<VFXId, GameObject>();
            foreach (var entry in _vfx)
            {
                if (entry.Prefab != null && !_vfxDictionary.ContainsKey(entry.Id))
                {
                    _vfxDictionary.Add(entry.Id, entry.Prefab);
                }
            }
        }

        public GameObject GetPrefab(VFXId id)
        {
            _vfxDictionary.TryGetValue(id, out var prefab);
            return prefab;
        }
    }
}
