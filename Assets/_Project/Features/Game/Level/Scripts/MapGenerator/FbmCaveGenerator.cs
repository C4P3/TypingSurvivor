using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "FbmCaveGenerator", menuName = "Typing Survivor/Map Generators/fBM Cave Generator")]
public class FbmCaveGenerator : ScriptableObject, IMapGenerator
{
    // Helper class for runtime calculations
    private class RuntimeNoiseLayer
    {
        public BlockTypeSetting settings;
        public float threshold;
    }

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
    [Tooltip("The preset defining the layers of terrain to be generated.")]
    [SerializeField] private TerrainPreset _terrainPreset;

    public List<TileData> Generate(long seed, Vector2Int worldOffset, Dictionary<TileBase, int> tileIdMap, Dictionary<string, TileBase> tileNameToTileMap)
    {
        var blockTiles = new List<TileData>();
        if (_terrainPreset == null || _terrainPreset.blockTypes == null || _terrainPreset.blockTypes.Count == 0) return blockTiles;

        var prng = new System.Random((int)seed);
        var noiseOffset = new Vector2(prng.Next(-10000, 10000), prng.Next(-10000, 10000));

        // --- 1. Calculate thresholds from weights ---
        var runtimeLayers = _terrainPreset.blockTypes.Select(bt => new RuntimeNoiseLayer { settings = bt }).ToList();
        float totalWeight = _terrainPreset.blockTypes.Sum(l => l.probabilityWeight);
        if (totalWeight <= 0) return blockTiles;

        float cumulativeWeight = 0f;
        foreach (var layer in runtimeLayers)
        {
            cumulativeWeight += layer.settings.probabilityWeight;
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
                BlockTypeSetting chosenBlock = null;
                foreach (var layer in runtimeLayers)
                {
                    if (finalNoise <= layer.threshold)
                    {
                        chosenBlock = layer.settings;
                        break;
                    }
                }
                if (chosenBlock == null) chosenBlock = runtimeLayers.Last().settings;

                // --- 4. Add tile data if it's not an empty layer ---
                if (chosenBlock != null && !string.IsNullOrEmpty(chosenBlock.tileName) && chosenBlock.tileName.ToUpper() != "EMPTY")
                {
                    if (tileNameToTileMap.TryGetValue(chosenBlock.tileName, out var tileAsset))
                    {
                        if (tileIdMap.TryGetValue(tileAsset, out int tileId))
                        {
                            blockTiles.Add(new TileData { Position = tilePos, TileId = tileId });
                        }
                    }
                }
            }
        }
        return blockTiles;
    }
}
