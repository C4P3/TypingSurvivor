using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A reusable setting for a block type, containing its name and probability weight.
/// This is used within TerrainPreset ScriptableObjects.
/// </summary>
[System.Serializable]
public class BlockTypeSetting
{
    [Tooltip("The name of the tile to be used, which must correspond to a tile in GameConfig's WorldTiles list.")]
    public string tileName;
    [Tooltip("The probability weight for this block type. Higher values are more likely to be chosen.")]
    public float probabilityWeight = 1.0f;
}

/// <summary>
/// A ScriptableObject that holds a reusable list of block types and their weights.
/// This can be shared across different map generators to define a consistent terrain composition.
/// </summary>
[CreateAssetMenu(fileName = "TerrainPreset", menuName = "Typing Survivor/Map Generators/Terrain Preset")]
public class TerrainPreset : ScriptableObject
{
    [Tooltip("A list of block types that define the composition of a terrain.")]
    public List<BlockTypeSetting> blockTypes;
}
