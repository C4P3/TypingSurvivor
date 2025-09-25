using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TypingSurvivor.Features.Game.Level
{
    /// <summary>
    /// マルチプレイ対戦用のスポーン戦略。
    /// 各プレイヤーができるだけ遠くなるようにスポーン地点を選ぶ。
    /// </summary>
    [CreateAssetMenu(fileName = "VersusSpawnStrategy", menuName = "TypingSurvivor/SpawnStrategy/Versus")]
    public class VersusSpawnStrategy : ScriptableObject, ISpawnPointStrategy
    {
        public List<Vector3Int> GetSpawnPoints(int playerCount, List<Vector3Int> walkableTiles, BoundsInt mapBounds)
        {
            var spawnPoints = new List<Vector3Int>();
            if (walkableTiles == null || !walkableTiles.Any() || playerCount <= 0) return spawnPoints;

            var candidates = new List<Vector3Int>(walkableTiles);
            var prng = new System.Random();

            // 1人目のスポーン地点をランダムに決定
            int firstIndex = prng.Next(candidates.Count);
            var firstPoint = candidates[firstIndex];
            spawnPoints.Add(firstPoint);
            candidates.RemoveAt(firstIndex);

            // 2人目以降を決定
            for (int i = 1; i < playerCount; i++)
            {
                if (!candidates.Any()) break;

                Vector3Int bestCandidate = Vector3Int.zero;
                float maxMinDistance = -1f;

                // 各候補タイルについて、既に確定した全スポーン地点からの最短距離を計算
                foreach (var candidate in candidates)
                {
                    float minDistanceToAnySpawn = float.MaxValue;
                    foreach (var spawnPoint in spawnPoints)
                    {
                        float dist = Vector3Int.Distance(candidate, spawnPoint);
                        if (dist < minDistanceToAnySpawn)
                        {
                            minDistanceToAnySpawn = dist;
                        }
                    }

                    // その最短距離が、今までの候補の中で最大なら、この候補を暫定の最適地点とする
                    if (minDistanceToAnySpawn > maxMinDistance)
                    {
                        maxMinDistance = minDistanceToAnySpawn;
                        bestCandidate = candidate;
                    }
                }
                
                spawnPoints.Add(bestCandidate);
                candidates.Remove(bestCandidate);
            }

            return spawnPoints;
        }
    }
}
