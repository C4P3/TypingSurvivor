using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TypingSurvivor.Features.Core.Leaderboard;
using TypingSurvivor.Features.Core.Leaderboard.Rating;
using TypingSurvivor.Features.UI.Common;
using UnityEngine;

namespace TypingSurvivor.Features.UI.Screens
{
    public abstract class RankingPanelBase : ScreenBase
    {
        [Header("Ranking Display")]
        [SerializeField] protected RankingEntry _rankingEntryPrefab;
        [SerializeField] protected Transform _listContainer;
        [SerializeField] protected GameObject _listSeparatorPrefab; // Optional prefab for a visual separator

        protected IRatingLeaderboardService _ratingLeaderboardService;
        protected ISurvivalLeaderboardService _survivalLeaderboardService;

        private readonly List<GameObject> _instantiatedObjects = new List<GameObject>();

        public virtual void Initialize(IRatingLeaderboardService ratingLeaderboardService, ISurvivalLeaderboardService survivalLeaderboardService)
        {
            _ratingLeaderboardService = ratingLeaderboardService;
            _survivalLeaderboardService = survivalLeaderboardService;
        }

        protected async Task RefreshLeaderboardAsync(
            Func<Task<(int playerRank, int totalPlayers)>> getPlayerRank,
            Func<int, int, Task<List<LeaderboardEntry>>> getLeaderboard)
        {
            ClearInstantiatedObjects();

            var playerRankData = await getPlayerRank();
            
            // If player is in the top 7, just show top 10.
            if (playerRankData.playerRank > 0 && playerRankData.playerRank <= 7)
            {
                var top10Entries = await getLeaderboard(0, 10);
                PopulateLeaderboard(top10Entries);
                return;
            }

            // Standard case: Top 5 + Player's neighbors
            var top5Entries = await getLeaderboard(0, 5);
            PopulateLeaderboard(top5Entries);

            List<LeaderboardEntry> neighborEntries = new List<LeaderboardEntry>();
            if (playerRankData.playerRank > 0)
            {
                int neighborOffset = Mathf.Max(0, playerRankData.playerRank - 3);
                neighborEntries = await getLeaderboard(neighborOffset, 5);
            }

            var topEntryNames = new HashSet<string>(top5Entries.Select(e => e.PlayerName));
            var filteredNeighborEntries = neighborEntries
                                                .Where(e => !topEntryNames.Contains(e.PlayerName))
                                                .ToList();

            if (top5Entries.Any() && filteredNeighborEntries.Any() && _listSeparatorPrefab != null)
            {
                var separator = Instantiate(_listSeparatorPrefab, _listContainer);
                _instantiatedObjects.Add(separator);
            }

            PopulateLeaderboard(filteredNeighborEntries);
        }

        protected void PopulateLeaderboard(List<LeaderboardEntry> entries)
        {
            foreach (var entryData in entries)
            {
                var entryUI = Instantiate(_rankingEntryPrefab, _listContainer);
                entryUI.Initialize(entryData.Rank, entryData.PlayerName, entryData.Score);
                _instantiatedObjects.Add(entryUI.gameObject);
                entryUI.gameObject.SetActive(true);
            }
        }

        protected void ClearInstantiatedObjects()
        {
            foreach (var obj in _instantiatedObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            _instantiatedObjects.Clear();
        }
    }
}
