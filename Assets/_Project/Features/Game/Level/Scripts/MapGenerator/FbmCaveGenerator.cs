using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Tilemaps;

/// <summary>
/// fBMノイズ生成における地形レイヤーの設定。
/// </summary>
[System.Serializable]
public class NoiseLayer
{
    [Tooltip("The name of the tile to be used. Use 'EMPTY' or leave blank for empty space (cave).")]
    public string tileName;
    [Tooltip("The probability weight for this layer. Higher values make this layer more common.")]
    public float probabilityWeight = 1.0f;

    // Calculated at runtime
    [System.NonSerialized]
    public float threshold;
}

[CreateAssetMenu(fileName = "FbmCaveGenerator", menuName = "Typing Survivor/Map Generators/fBM Cave Generator")]
public class FbmCaveGenerator : ScriptableObject, IMapGenerator
{
    [Header("Map Dimensions")]
    [SerializeField] private int _width = 100;
    [SerializeField] private int _height = 100;

    [Header("fBM Noise Settings")]
    [Tooltip("Overall scale of the noise. Smaller values create larger caves.")]
    [SerializeField] private float _noiseScale = 0.1f;
    [Tooltip("Number of noise layers to stack. More octaves add more detail.")]
    [Range(1, 8)]
    [SerializeField] private int _octaves = 4;
    [Tooltip("How much detail is added in each octave (frequency multiplier).")]
    [SerializeField] private float _lacunarity = 2.0f;
    [Tooltip("How much each octave contributes to the overall shape (amplitude multiplier).")]
    [Range(0, 1)]
    [SerializeField] private float _persistence = 0.5f;

    [Header("Terrain Layers")]
    [Tooltip("Layers of terrain, ordered from lowest elevation (e.g., empty space) to highest (e.g., solid rock).")]
    [SerializeField] private NoiseLayer[] _layers;

    public List<TileData> Generate(long seed, Vector2Int worldOffset, Dictionary<TileBase, int> tileIdMap, Dictionary<string, TileBase> tileNameToTileMap)
    {
        var blockTiles = new List<TileData>();
        if (_layers == null || _layers.Length == 0) return blockTiles;

        var prng = new System.Random((int)seed);
        var noiseOffset = new Vector2(prng.Next(-10000, 10000), prng.Next(-10000, 10000));

        // --- 1. Calculate thresholds from weights ---
        float totalWeight = _layers.Sum(l => l.probabilityWeight);
        if (totalWeight <= 0) return blockTiles; // Prevent division by zero

        float cumulativeWeight = 0f;
        foreach (var layer in _layers)
        {
            cumulativeWeight += layer.probabilityWeight;
            layer.threshold = cumulativeWeight / totalWeight;
        }

        // --- 2. Generate map using fBM noise ---
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var tilePos = new Vector3Int(x - _width / 2 + worldOffset.x, y - _height / 2 + worldOffset.y, 0);

                // --- fBM Calculation ---
                float totalNoise = 0;
                float frequency = _noiseScale;
                float amplitude = 1.0f;
                float maxAmplitude = 0;

                for (int i = 0; i < _octaves; i++)
                {
                    float sampleX = (tilePos.x + noiseOffset.x) * frequency;
                    float sampleY = (tilePos.y + noiseOffset.y) * frequency;
                    totalNoise += Mathf.PerlinNoise(sampleX, sampleY) * amplitude;
                    
                    maxAmplitude += amplitude;
                    amplitude *= _persistence;
                    frequency *= _lacunarity;
                }
                float finalNoise = totalNoise / maxAmplitude;
                // --- End fBM ---

                // --- 3. Choose tile based on noise and thresholds ---
                NoiseLayer chosenLayer = null;
                foreach (var layer in _layers)
                {
                    if (finalNoise <= layer.threshold)
                    {
                        chosenLayer = layer;
                        break;
                    }
                }
                // Fallback to the last layer if something goes wrong
                if (chosenLayer == null) chosenLayer = _layers.Last();

                // --- 4. Add tile data if it's not an empty layer ---
                if (chosenLayer != null && !string.IsNullOrEmpty(chosenLayer.tileName) && chosenLayer.tileName.ToUpper() != "EMPTY")
                {
                    if (tileNameToTileMap.TryGetValue(chosenLayer.tileName, out var tileAsset))
                    {
                        if (tileIdMap.TryGetValue(tileAsset, out int tileId))
                        {
                            blockTiles.Add(new TileData { Position = tilePos, TileId = tileId });
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[FbmCaveGenerator] Tile with name '{chosenLayer.tileName}' not found in the provided tile map.");
                    }
                }
            }
        }
        return blockTiles;
    }
}
