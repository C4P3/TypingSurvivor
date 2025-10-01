using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TypingSurvivor.Features.Game.Level
{
    /// <summary>
    [CreateAssetMenu(fileName = "AreaSpawnStrategy", menuName = "Typing Survivor/SpawnStrategy/Area Strategy")]
    public class VersusSpawnStrategy : ScriptableObject, ISpawnPointStrategy
    {
        [Tooltip("エリアの中心からどのくらいの範囲をスポーン候補とするか（パーセンテージ）")]
        [Range(0.1f, 1.0f)]
        [SerializeField] private float _centerAreaRatio = 0.8f;

        public List<Vector3Int> GetSpawnPoints(int playerCount, List<Vector3Int> areaWalkableTiles, BoundsInt areaBounds, Vector2Int worldOffset)
        {
            var spawnPoints = new List<Vector3Int>();
            if (areaWalkableTiles == null || !areaWalkableTiles.Any() || playerCount <= 0) return spawnPoints;

            // エリアの中心を計算
            float centerX = areaBounds.center.x;
            float centerY = areaBounds.center.y;
            float width = areaBounds.size.x * _centerAreaRatio;
            float height = areaBounds.size.y * _centerAreaRatio;
            var centerBounds = new Bounds(new Vector3(centerX, centerY, 0), new Vector3(width, height, 0));

            // 中心エリア内の歩行可能なタイルを候補とする
            var candidateTiles = areaWalkableTiles.Where(p => centerBounds.Contains(p)).ToList();
            if (candidateTiles.Count < playerCount)
            {
                // 候補が足りない場合は全域から選ぶ
                candidateTiles = new List<Vector3Int>(areaWalkableTiles);
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
