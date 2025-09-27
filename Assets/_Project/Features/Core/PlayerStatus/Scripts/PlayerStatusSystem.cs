using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TypingSurvivor.Features.Core.PlayerStatus
{
    public class PlayerStatusSystem : IPlayerStatusSystemWriter, IPlayerStatusSystemReader
    {
        private class PlayerStats
        {
            public readonly List<StatModifier> Modifiers = new();
        }

        private readonly Dictionary<ulong, PlayerStats> _playerStats = new();
        private readonly PlayerDefaultStats _defaultStats;

        public PlayerStatusSystem(PlayerDefaultStats defaultStats)
        {
            Debug.Assert(defaultStats != null, "PlayerDefaultStats cannot be null.");
            _defaultStats = defaultStats;
        }

        public void ApplyModifier(ulong clientId, StatModifier modifier)
        {
            if (!_playerStats.ContainsKey(clientId))
            {
                _playerStats[clientId] = new PlayerStats();
            }

            if (!modifier.IsPermanentDuration)
            {
                modifier.SetEndTime(Time.time);
            }

            _playerStats[clientId].Modifiers.Add(modifier);
        }

        public float GetStatValue(ulong clientId, PlayerStat stat)
        {
            float baseValue = _defaultStats.GetBaseStatValue(stat);

            if (!_playerStats.ContainsKey(clientId))
            {
                return baseValue;
            }

            var modifiers = _playerStats[clientId].Modifiers;
            float additiveBonus = 0f;
            float multiplicativeBonus = 1.0f;

            // LINQで書くこともできるが、パフォーマンスと可読性のためにループを使用
            foreach (var mod in modifiers)
            {
                if (mod.Stat != stat) continue;

                if (mod.Type == ModifierType.Additive)
                {
                    additiveBonus += mod.Value;
                }
                else if (mod.Type == ModifierType.Multiplicative)
                {
                    multiplicativeBonus *= mod.Value;
                }
            }

            // 計算順序: (基本値 + 加算値) * 乗算値
            return (baseValue + additiveBonus) * multiplicativeBonus;
        }

        public void Update()
        {
            // 期限切れの一時的なModifierを削除する
            float currentTime = Time.time;
            foreach (var stats in _playerStats.Values)
            {
                stats.Modifiers.RemoveAll(mod => !mod.IsPermanentDuration && currentTime > mod.EndTime);
            }
        }

        public void ClearSessionModifiers(ulong clientId)
        {
            if (_playerStats.TryGetValue(clientId, out var stats))
            {
                stats.Modifiers.RemoveAll(mod => mod.Scope == ModifierScope.Session);
            }
        }

        public void ClearAllModifiers(ulong clientId)
        {
            if (_playerStats.TryGetValue(clientId, out var stats))
            {
                stats.Modifiers.Clear();
            }
        }
    }
}
