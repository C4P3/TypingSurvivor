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

        public List<Vector3Int> GetSpawnPoints(int playerCount, List<Vector3Int> areaWalkableTiles, BoundsInt areaBounds, Vector2Int worldOffset)
        {
            var spawnPoints = new List<Vector3Int>();
            if (areaWalkableTiles == null || !areaWalkableTiles.Any()) return spawnPoints;

            // マップの中心エリアを計算
            float centerX = areaBounds.center.x;
            float centerY = areaBounds.center.y;
            float width = areaBounds.size.x * _centerAreaRatio;
            float height = areaBounds.size.y * _centerAreaRatio;

            var centerBounds = new Bounds(new Vector3(centerX, centerY, 0), new Vector3(width, height, 0));

            // 中心エリア内の歩行可能なタイルを候補とする
            var candidateTiles = areaWalkableTiles.Where(p => centerBounds.Contains(p)).ToList();
            if (!candidateTiles.Any())
            {
                // 候補がない場合は全域から選ぶ
                candidateTiles = areaWalkableTiles;
            }

            // 候補の中からランダムに必要数を選択
            var prng = new System.Random();
            for (int i = 0; i < playerCount && candidateTiles.Any(); i++)
            {
                int index = prng.Next(candidateTiles.Count);
                spawnPoints.Add(candidateTiles[index]);
                candidateTiles.RemoveAt(index);
            }

            return spawnPoints;
        }
    }
}
