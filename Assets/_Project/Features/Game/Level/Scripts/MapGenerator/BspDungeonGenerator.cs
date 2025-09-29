using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "BspDungeonGenerator", menuName = "Typing Survivor/Map Generators/BSP Dungeon Generator")]
public class BspDungeonGenerator : ScriptableObject, IMapGenerator
{
    // Helper class to represent a partition of space
    private class BspLeaf
    {
        public RectInt rect;
        public BspLeaf leftChild;
        public BspLeaf rightChild;
        public RectInt room;
        public List<RectInt> corridors = new List<RectInt>();

        public BspLeaf(RectInt rect)
        {
            this.rect = rect;
        }

        public bool Split(int minSize, System.Random prng)
        {
            if (leftChild != null || rightChild != null)
                return false; // Already split

            bool splitH = prng.NextDouble() > 0.5;
            if (rect.width > rect.height && (float)rect.width / rect.height >= 1.25f)
                splitH = false;
            else if (rect.height > rect.width && (float)rect.height / rect.width >= 1.25f)
                splitH = true;

            int max = (splitH ? rect.height : rect.width) - minSize;
            if (max <= minSize)
                return false; // Too small to split

            int split = prng.Next(minSize, max);

            if (splitH)
            {
                leftChild = new BspLeaf(new RectInt(rect.x, rect.y, rect.width, split));
                rightChild = new BspLeaf(new RectInt(rect.x, rect.y + split, rect.width, rect.height - split));
            }
            else
            {
                leftChild = new BspLeaf(new RectInt(rect.x, rect.y, split, rect.height));
                rightChild = new BspLeaf(new RectInt(rect.x + split, rect.y, rect.width - split, rect.height));
            }
            return true;
        }

        public void CreateRooms(int minRoomSize, System.Random prng)
        {
            if (leftChild != null || rightChild != null)
            {
                leftChild?.CreateRooms(minRoomSize, prng);
                rightChild?.CreateRooms(minRoomSize, prng);

                if (leftChild != null && rightChild != null)
                    CreateCorridor(leftChild, rightChild, prng);
            }
            else
            {
                Vector2Int roomSize = new Vector2Int(prng.Next(minRoomSize, rect.width - 1), prng.Next(minRoomSize, rect.height - 1));
                Vector2Int roomPos = new Vector2Int(prng.Next(1, rect.width - roomSize.x - 1), prng.Next(1, rect.height - roomSize.y - 1));
                room = new RectInt(rect.x + roomPos.x, rect.y + roomPos.y, roomSize.x, roomSize.y);
            }
        }

        private void CreateCorridor(BspLeaf left, BspLeaf right, System.Random prng)
        {
            RectInt lroom = left.GetRoom();
            RectInt rroom = right.GetRoom();

            Vector2Int lcenter = Vector2Int.RoundToInt(lroom.center);
            Vector2Int rcenter = Vector2Int.RoundToInt(rroom.center);

            if (prng.NextDouble() > 0.5)
            {
                corridors.Add(new RectInt(lcenter.x, lcenter.y, rcenter.x - lcenter.x, 1));
                corridors.Add(new RectInt(rcenter.x, lcenter.y, 1, rcenter.y - lcenter.y));
            }
            else
            {
                corridors.Add(new RectInt(lcenter.x, lcenter.y, 1, rcenter.y - lcenter.y));
                corridors.Add(new RectInt(lcenter.x, rcenter.y, rcenter.x - lcenter.x, 1));
            }
        }

        public RectInt GetRoom()
        {
            if (room.width > 0 && room.height > 0)
                return room;
            
            RectInt lroom = leftChild?.GetRoom() ?? new RectInt();
            RectInt rroom = rightChild?.GetRoom() ?? new RectInt();

            if (lroom.width == 0 && lroom.height == 0) return rroom;
            if (rroom.width == 0 && rroom.height == 0) return lroom;
            
            return Random.value > 0.5f ? lroom : rroom;
        }
    }

    [Header("Map Dimensions")]
    [SerializeField] private int _width = 100;
    [SerializeField] private int _height = 100;

    [Header("Tile Settings")]
    [Tooltip("The terrain preset to use. The FIRST block type will be the wall, the SECOND will be the floor.")]
    [SerializeField] private TerrainPreset _terrainPreset;

    [Header("BSP Settings")]
    [SerializeField] private int _minLeafSize = 20;
    [SerializeField] private int _minRoomSize = 5;

    public List<TileData> Generate(long seed, Vector2Int worldOffset, Dictionary<TileBase, int> tileIdMap, Dictionary<string, TileBase> tileNameToTileMap)
    {
        var blockTiles = new List<TileData>();
        if (_terrainPreset == null || _terrainPreset.blockTypes == null || _terrainPreset.blockTypes.Count < 1) return blockTiles;

        var wallTypeName = _terrainPreset.blockTypes[0].tileName;
        if (!tileNameToTileMap.TryGetValue(wallTypeName, out var wallTileAsset) || !tileIdMap.TryGetValue(wallTileAsset, out var wallTileId))
        {
            Debug.LogError($"[BspDungeonGenerator] Wall tile '{wallTypeName}' not found.");
            return blockTiles;
        }

        var prng = new System.Random((int)seed);
        var root = new BspLeaf(new RectInt(0, 0, _width, _height));
        var leaves = new List<BspLeaf> { root };

        bool didSplit = true;
        while (didSplit)
        {
            didSplit = false;
            var newLeaves = new List<BspLeaf>();
            foreach (var leaf in leaves)
            {
                if (leaf.leftChild == null && leaf.rightChild == null)
                {
                    if (leaf.rect.width > _minLeafSize || leaf.rect.height > _minLeafSize)
                    {
                        if (leaf.Split(_minLeafSize, prng))
                        {
                            newLeaves.Add(leaf.leftChild);
                            newLeaves.Add(leaf.rightChild);
                            didSplit = true;
                        }
                        else
                        {
                            newLeaves.Add(leaf);
                        }
                    }
                    else
                    {
                        newLeaves.Add(leaf);
                    }
                }
            }
            leaves = newLeaves;
        }

        root.CreateRooms(_minRoomSize, prng);

        bool[,] map = new bool[_width, _height]; // true for floor, false for wall
        foreach (var leaf in leaves)
        {
            if (leaf.room.width > 0 && leaf.room.height > 0)
            {
                for (int x = leaf.room.xMin; x < leaf.room.xMax; x++)
                    for (int y = leaf.room.yMin; y < leaf.room.yMax; y++)
                        map[x, y] = true;
            }
            foreach (var corridor in leaf.corridors)
            {
                for (int x = corridor.xMin; x < corridor.xMax; x++)
                    for (int y = corridor.yMin; y < corridor.yMax; y++)
                        map[x, y] = true;
            }
        }

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (!map[x, y])
                {
                    var tilePos = new Vector3Int(x - _width / 2 + worldOffset.x, y - _height / 2 + worldOffset.y, 0);
                    blockTiles.Add(new TileData { Position = tilePos, TileId = wallTileId });
                }
            }
        }
        return blockTiles;
    }
}
