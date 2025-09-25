using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TypingSurvivor.Features.Game.Level
{
    /// <summary>
    /// シングルプレイ用のスポーン戦略。
    /// マップ中央付近のランダムな地点を返す。
    /// </summary>
    [CreateAssetMenu(fileName = "SinglePlayerSpawnStrategy", menuName = "TypingSurvivor/SpawnStrategy/SinglePlayer")]
    public class SinglePlayerSpawnStrategy : ScriptableObject, ISpawnPointStrategy
    {
        [Tooltip("マップの中心からどのくらいの範囲をスポーン候補とするか（パーセンテージ）")]
        [Range(0.1f, 1.0f)]
        [SerializeField] private float _centerAreaRatio = 0.5f;

        public List<Vector3Int> GetSpawnPoints(int playerCount, List<Vector3Int> walkableTiles, BoundsInt mapBounds)
        {
            var spawnPoints = new List<Vector3Int>();
            if (walkableTiles == null || !walkableTiles.Any()) return spawnPoints;

            // マップの中心エリアを計算
            float centerX = mapBounds.center.x;
            float centerY = mapBounds.center.y;
            float width = mapBounds.size.x * _centerAreaRatio;
            float height = mapBounds.size.y * _centerAreaRatio;

            var centerBounds = new Bounds(new Vector3(centerX, centerY, 0), new Vector3(width, height, 0));

            // 中心エリア内の歩行可能なタイルを候補とする
            var candidateTiles = walkableTiles.Where(p => centerBounds.Contains(p)).ToList();
            if (!candidateTiles.Any())
            {
                // 候補がない場合は全域から選ぶ
                candidateTiles = walkableTiles;
            }

            // 候補の中からランダムに1点を選択
            var prng = new System.Random();
            var spawnPoint = candidateTiles[prng.Next(candidateTiles.Count)];
            spawnPoints.Add(spawnPoint);

            return spawnPoints;
        }
    }
}
