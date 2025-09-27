using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;
using TypingSurvivor.Features.Game.Player;
using TypingSurvivor.Features.Game.Level;
using TypingSurvivor.Features.Game.Level.Data;

/// <summary>
/// Level (タイルマップ) の状態を管理し、変更ロジックを実行するクラス。
/// サーバーサイドで動作し、状態をクライアントに同期する。
/// </summary>
public class LevelManager : NetworkBehaviour, ILevelService
{
    #region Events
    public event Action<ulong, Vector3Int> OnBlockDestroyed_Server;
    #endregion

    #region Serialized Fields (Inspector)
    [Header("References")]
    [SerializeField] private Tilemap _blockTilemap;
    [SerializeField] private Tilemap _itemTilemap;

    // --- Dependencies (Injected by Bootstrapper) ---
    private IMapGenerator _mapGenerator;
    private IItemPlacementStrategy _itemPlacementStrategy;
    private ItemRegistry _itemRegistry;

    [Header("Map Settings")]
    [SerializeField] private long _mapSeed;
    [SerializeField] private bool _useRandomSeed = true;

    [Header("Chunk System Settings")]
    [SerializeField] private int _chunkSize = 16;
    [Tooltip("プレイヤーの周囲何チャンク分を同期対象にするか")]
    [SerializeField] private int _generationRadiusInChunks = 2;
    #endregion

    #region Private Fields
    // --- Tile ID Management ---
    private Dictionary<TileBase, int> _tileToBaseIdMap;
    private List<TileBase> _tileIdMap;

    // --- Server-Side Data ---
    private readonly Dictionary<Vector2Int, List<TileData>> _entireBlockMapData_Server = new();
    private readonly Dictionary<Vector2Int, List<TileData>> _entireItemMapData_Server = new();
    private readonly Dictionary<ulong, Vector2Int> _playerChunkPositions_Server = new();
    private readonly HashSet<Vector2Int> _activeChunks_Server = new();

    // --- Synced Data ---
    private readonly NetworkList<TileData> _activeBlockTiles = new();
    private readonly NetworkList<TileData> _activeItemTiles = new();
    #endregion

    #region Initialization
    public void Initialize(IMapGenerator mapGenerator, IItemPlacementStrategy itemPlacementStrategy, ItemRegistry itemRegistry)
    {
        _mapGenerator = mapGenerator;
        _itemPlacementStrategy = itemPlacementStrategy;
        _itemRegistry = itemRegistry;

        // ジェネレーターが使用するブロックタイルと、ItemRegistryにあるアイテムタイルの両方からIDマップを動的に生成
        _tileIdMap = new List<TileBase>();
        _tileToBaseIdMap = new Dictionary<TileBase, int>();
        
        var allTiles = new List<TileBase>();
        if(_mapGenerator != null && _mapGenerator.AllTiles != null) allTiles.AddRange(_mapGenerator.AllTiles);
        if(_itemRegistry != null && _itemRegistry.AllItems != null)
        {
            allTiles.AddRange(_itemRegistry.AllItems.Select(item => item.itemTile));
        }

        foreach (var tile in allTiles.Distinct())
        {
            if (tile != null && !_tileToBaseIdMap.ContainsKey(tile))
            {
                _tileToBaseIdMap[tile] = _tileIdMap.Count;
                _tileIdMap.Add(tile);
            }
        }
    }
    #endregion

    #region Unity Lifecycle & Netcode Callbacks
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            PlayerFacade.OnPlayerSpawned_Server += HandlePlayerSpawned;
            PlayerFacade.OnPlayerDespawned_Server += HandlePlayerDespawned;
            PlayerFacade.OnPlayerMoved_Server += HandlePlayerMoved;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                HandleClientConnected(client.ClientId);
            }
        }

        if (IsClient)
        {
            _activeBlockTiles.OnListChanged += OnBlockTilesChanged;
            _activeItemTiles.OnListChanged += OnItemTilesChanged;
            InitialDrawAllTiles();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            _activeBlockTiles.OnListChanged -= OnBlockTilesChanged;
            _activeItemTiles.OnListChanged -= OnItemTilesChanged;
        }

        if (IsServer)
        {
            PlayerFacade.OnPlayerSpawned_Server -= HandlePlayerSpawned;
            PlayerFacade.OnPlayerDespawned_Server -= HandlePlayerDespawned;
            PlayerFacade.OnPlayerMoved_Server -= HandlePlayerMoved;
        }
    }
    #endregion
    
    #region Public API

    public void GenerateWorld(MapGenerationRequest request)
    {
        if (!IsServer) return;

        // Clear all previous map data
        _entireBlockMapData_Server.Clear();
        _entireItemMapData_Server.Clear();
        _activeBlockTiles.Clear();
        _activeItemTiles.Clear();
        _activeChunks_Server.Clear();

        if (_useRandomSeed && request.BaseSeed == 0)
        {
            _mapSeed = System.DateTime.Now.Ticks;
        }
        else
        {
            _mapSeed = request.BaseSeed;
        }
        
        var prng = new System.Random((int)_mapSeed);

        // Generate each area defined in the request
        foreach (var area in request.SpawnAreas)
        {
            if (area.MapGenerator == null || area.ItemPlacementStrategy == null)
            {
                Debug.LogError("A SpawnArea in the MapGenerationRequest has a null MapGenerator or ItemPlacementStrategy. Skipping this area.");
                continue;
            }
            var generatedBlocks = area.MapGenerator.Generate(_mapSeed, area.WorldOffset, _tileToBaseIdMap);
            var generatedItems = area.ItemPlacementStrategy.PlaceItems(generatedBlocks, _itemRegistry, prng, _tileToBaseIdMap, area.WorldOffset);

            ChunkAndStoreMapData(generatedBlocks, _entireBlockMapData_Server);
            ChunkAndStoreMapData(generatedItems, _entireItemMapData_Server);
        }
        
        UpdateActiveChunks();
    }

    public List<Vector3Int> GetSpawnPoints(SpawnArea spawnArea)
    {
        if (!IsServer) return new List<Vector3Int>();

        // Extract walkable tiles and calculate bounds for the specific area
        var areaWalkableTiles = new List<Vector3Int>();
        var areaBounds = new BoundsInt();
        bool firstTile = true;

        // This is inefficient, but works for now. A better approach would be to query chunks in the area.
        foreach (var tileData in _entireBlockMapData_Server.SelectMany(kvp => kvp.Value))
        {
            // A simple way to check if a tile is within the conceptual bounds of the spawn area
            if (Vector2.Distance(new Vector2(tileData.Position.x, tileData.Position.y), spawnArea.WorldOffset) < 100) // Assuming area size is around 100
            {
                if (firstTile)
                {
                    areaBounds.position = tileData.Position;
                    firstTile = false;
                }
                else
                {
                    areaBounds.xMin = Mathf.Min(areaBounds.xMin, tileData.Position.x);
                    areaBounds.yMin = Mathf.Min(areaBounds.yMin, tileData.Position.y);
                    areaBounds.xMax = Mathf.Max(areaBounds.xMax, tileData.Position.x + 1);
                    areaBounds.yMax = Mathf.Max(areaBounds.yMax, tileData.Position.y + 1);
                }
            }
        }
        
        // Find all walkable tiles within the calculated bounds
        for (int x = areaBounds.xMin; x < areaBounds.xMax; x++)
        {
            for (int y = areaBounds.yMin; y < areaBounds.yMax; y++)
            {
                var pos = new Vector3Int(x, y, 0);
                if (IsWalkable(pos))
                {
                    areaWalkableTiles.Add(pos);
                }
            }
        }

        return spawnArea.SpawnPointStrategy.GetSpawnPoints(spawnArea.PlayerClientIds.Count, areaWalkableTiles, areaBounds, spawnArea.WorldOffset);
    }

    #endregion

    #region ILevelService Implementation (Server-side Logic)

    public TileBase GetTile(Vector3Int gridPosition)
    {
        if (!IsServer) return null;

        Vector2Int chunkPos = WorldToChunkPos(gridPosition);
        
        if (_entireItemMapData_Server.TryGetValue(chunkPos, out var itemTiles))
        {
            foreach (var tileData in itemTiles)
            {
                if (tileData.Position == gridPosition) return _tileIdMap[tileData.TileId];
            }
        }
        
        if (_entireBlockMapData_Server.TryGetValue(chunkPos, out var blockTiles))
        {
            foreach (var tileData in blockTiles)
            {
                if (tileData.Position == gridPosition) return _tileIdMap[tileData.TileId];
            }
        }

        return null;
    }

    public void DestroyBlock(ulong clientId, Vector3Int gridPosition)
    {
        if (!IsServer) return;

        Vector2Int chunkPos = WorldToChunkPos(gridPosition);
        if (_entireBlockMapData_Server.TryGetValue(chunkPos, out var tiles))
        {
            tiles.RemoveAll(t => t.Position == gridPosition);
        }

        for(int i = _activeBlockTiles.Count - 1; i >= 0; i--)
        {
            if(_activeBlockTiles[i].Position == gridPosition)
            {
                _activeBlockTiles.RemoveAt(i);
                break;
            }
        }
        OnBlockDestroyed_Server?.Invoke(clientId, gridPosition);
    }

    public void RemoveItem(Vector3Int gridPosition)
    {
        if (!IsServer) return;

        Vector2Int chunkPos = WorldToChunkPos(gridPosition);
        if (_entireItemMapData_Server.TryGetValue(chunkPos, out var tiles))
        {
            tiles.RemoveAll(t => t.Position == gridPosition);
        }

        for (int i = _activeItemTiles.Count - 1; i >= 0; i--)
        {
            if (_activeItemTiles[i].Position == gridPosition)
            {
                _activeItemTiles.RemoveAt(i);
                break;
            }
        }
    }

    public bool IsWalkable(Vector3Int gridPosition)
    {
        if (!IsServer) return true;

        Vector2Int chunkPos = WorldToChunkPos(gridPosition);
        if (_entireBlockMapData_Server.TryGetValue(chunkPos, out var tiles))
        {
            return !tiles.Any(t => t.Position == gridPosition);
        }
        return true;
    }

    public bool HasItemTile(Vector3Int gridPosition)
    {
        if (!IsServer) return false;
        
        Vector2Int chunkPos = WorldToChunkPos(gridPosition);
        if (_entireItemMapData_Server.TryGetValue(chunkPos, out var tiles))
        {
            return tiles.Any(t => t.Position == gridPosition);
        }
        return false;
    }
    
    public void ClearArea(Vector3Int gridPosition, int radius)
    {
        if (!IsServer) return;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                var targetPos = new Vector3Int(gridPosition.x + x, gridPosition.y + y, gridPosition.z);
                
                if (GetTile(targetPos) != null)
                {
                    RemoveItem(targetPos);
                    DestroyBlock(0, targetPos);
                }
            }
        }
    }

    #endregion

    #region Server-Side Chunk Management

    private void ChunkAndStoreMapData(List<TileData> tiles, Dictionary<Vector2Int, List<TileData>> targetStorage)
    {
        foreach (var tile in tiles)
        {
            Vector2Int chunkPos = WorldToChunkPos(tile.Position);
            if (!targetStorage.ContainsKey(chunkPos))
            {
                targetStorage[chunkPos] = new List<TileData>();
            }
            targetStorage[chunkPos].Add(tile);
        }
    }
    
    private void UpdateActiveChunks()
    {
        var allRequiredChunks = new HashSet<Vector2Int>();
        foreach (var playerChunk in _playerChunkPositions_Server.Values)
        {
            for (int x = -_generationRadiusInChunks; x <= _generationRadiusInChunks; x++)
            {
                for (int y = -_generationRadiusInChunks; y <= _generationRadiusInChunks; y++)
                {
                    allRequiredChunks.Add(new Vector2Int(playerChunk.x + x, playerChunk.y + y));
                }
            }
        }

        var chunksToUnload = _activeChunks_Server.Except(allRequiredChunks).ToList();
        foreach (var chunkPos in chunksToUnload)
        {
            UnloadChunk(chunkPos);
            _activeChunks_Server.Remove(chunkPos);
        }

        var chunksToLoad = allRequiredChunks.Except(_activeChunks_Server).ToList();
        foreach (var chunkPos in chunksToLoad)
        {
            LoadChunk(chunkPos);
            _activeChunks_Server.Add(chunkPos);
        }
    }

    private void LoadChunk(Vector2Int chunkPos)
    {
        if (_entireBlockMapData_Server.TryGetValue(chunkPos, out var blockTiles))
        {
            foreach (var tile in blockTiles) _activeBlockTiles.Add(tile);
        }
        if (_entireItemMapData_Server.TryGetValue(chunkPos, out var itemTiles))
        {
            foreach (var tile in itemTiles) _activeItemTiles.Add(tile);
        }
    }

    private void UnloadChunk(Vector2Int chunkPos)
    {
        UnloadTilesFromNetworkList(_activeBlockTiles, chunkPos);
        UnloadTilesFromNetworkList(_activeItemTiles, chunkPos);
    }
    
    private void UnloadTilesFromNetworkList(NetworkList<TileData> tileList, Vector2Int chunkPos)
    {
        for (int i = tileList.Count - 1; i >= 0; i--)
        {
            if (WorldToChunkPos(tileList[i].Position) == chunkPos)
            {
                tileList.RemoveAt(i);
            }
        }
    }

    #endregion

    #region Event Handlers & Connection
    
    private void HandlePlayerMoved(ulong clientId, Vector3 newPosition)
    {
        Vector2Int currentChunk = WorldToChunkPos(newPosition);
        if (_playerChunkPositions_Server.TryGetValue(clientId, out var previousChunk))
        {
            if (previousChunk != currentChunk)
            {
                _playerChunkPositions_Server[clientId] = currentChunk;
                UpdateActiveChunks();
            }
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        _playerChunkPositions_Server[clientId] = Vector2Int.zero;
        UpdateActiveChunks();
    }
    
    private void HandlePlayerSpawned(ulong clientId, Vector3 spawnPosition)
    {
        _playerChunkPositions_Server[clientId] = WorldToChunkPos(spawnPosition);
        UpdateActiveChunks();
    }
    
    private void HandlePlayerDespawned(ulong clientId)
    {
        if (_playerChunkPositions_Server.Remove(clientId))
        {
            UpdateActiveChunks();
        }
    }
    
    #endregion


    #region Client-Side View Updates & Helpers
    
    private void OnBlockTilesChanged(NetworkListEvent<TileData> changeEvent)
    {
        ProcessTileChange(changeEvent, _blockTilemap);
    }

    private void OnItemTilesChanged(NetworkListEvent<TileData> changeEvent)
    {
        ProcessTileChange(changeEvent, _itemTilemap);
    }

    private void ProcessTileChange(NetworkListEvent<TileData> changeEvent, Tilemap targetMap)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<TileData>.EventType.Add:
            case NetworkListEvent<TileData>.EventType.Insert:
                DrawTile(changeEvent.Value, targetMap);
                break;
            case NetworkListEvent<TileData>.EventType.Remove:
            case NetworkListEvent<TileData>.EventType.RemoveAt:
                ClearTile(changeEvent.Value, targetMap);
                break;
            case NetworkListEvent<TileData>.EventType.Clear:
                targetMap.ClearAllTiles();
                break;
        }
    }

    private void InitialDrawAllTiles()
    {
        _blockTilemap.ClearAllTiles();
        _itemTilemap.ClearAllTiles();
        foreach (var tileData in _activeBlockTiles) DrawTile(tileData, _blockTilemap);
        foreach (var tileData in _activeItemTiles) DrawTile(tileData, _itemTilemap);
    }

    private void DrawTile(TileData data, Tilemap targetMap)
    {
        if (data.TileId >= 0 && data.TileId < _tileIdMap.Count)
        {
            targetMap.SetTile(data.Position, _tileIdMap[data.TileId]);
        }
    }
    
    private void ClearTile(TileData data, Tilemap targetMap)
    {
        targetMap.SetTile(data.Position, null);
    }
    
    private Vector2Int WorldToChunkPos(Vector3 worldPos)
    {
        Vector3Int gridPos = _blockTilemap.WorldToCell(worldPos);
        return new Vector2Int(Mathf.FloorToInt((float)gridPos.x / _chunkSize), Mathf.FloorToInt((float)gridPos.y / _chunkSize));
    }
    
    private Vector2Int WorldToChunkPos(Vector3Int gridPos)
    {
        return new Vector2Int(Mathf.FloorToInt((float)gridPos.x / _chunkSize), Mathf.FloorToInt((float)gridPos.y / _chunkSize));
    }

    #endregion
}