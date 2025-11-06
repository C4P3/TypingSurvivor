using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace TypingSurvivor.Features.Core.PlayerStatus
{
    public class PlayerStatusSystem : IPlayerStatusSystemWriter, IPlayerStatusSystemReader
    {
        // ulong: clientId, PlayerStat: stat, float: value
        public event Action<ulong, PlayerStat, float> OnStatChanged;

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
            if (!NetworkManager.Singleton.IsServer) return;

            if (!_playerStats.ContainsKey(clientId))
            {
                _playerStats[clientId] = new PlayerStats();
            }

            if (!modifier.IsPermanentDuration)
            {
                modifier.SetEndTime((float) NetworkManager.Singleton.ServerTime.Time);
            }

            _playerStats[clientId].Modifiers.Add(modifier);
            RecalculateAndNotify(clientId, modifier.Stat);
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
            if (!NetworkManager.Singleton.IsServer) return;

            // 期限切れの一時的なModifierを削除する
            float currentTime = (float)NetworkManager.Singleton.ServerTime.Time;
            foreach (var (clientId, stats) in _playerStats)
            {
                var expiredStats = stats.Modifiers
                    .Where(mod => !mod.IsPermanentDuration && currentTime > mod.EndTime)
                    .Select(mod => mod.Stat)
                    .Distinct()
                    .ToList();

                if (expiredStats.Count > 0)
                {
                    stats.Modifiers.RemoveAll(mod => !mod.IsPermanentDuration && currentTime > mod.EndTime);
                    foreach (var stat in expiredStats)
                    {
                        RecalculateAndNotify(clientId, stat);
                    }
                }
            }
        }

        public void ClearSessionModifiers(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            if (_playerStats.TryGetValue(clientId, out var stats))
            {
                stats.Modifiers.RemoveAll(mod => mod.Scope == ModifierScope.Session);
                RecalculateAndNotifyAll(clientId);
            }
        }

        public void ClearAllModifiers(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            if (_playerStats.TryGetValue(clientId, out var stats))
            {
                stats.Modifiers.Clear();
                RecalculateAndNotifyAll(clientId);
            }
        }

        private void RecalculateAndNotify(ulong clientId, PlayerStat stat)
        {
            float newValue = GetStatValue(clientId, stat);
            OnStatChanged?.Invoke(clientId, stat, newValue);
        }

        private void RecalculateAndNotifyAll(ulong clientId)
        {
            foreach (PlayerStat stat in Enum.GetValues(typeof(PlayerStat)))
            {
                RecalculateAndNotify(clientId, stat);
            }
        }
    }
}
