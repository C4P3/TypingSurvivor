using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;
using TypingSurvivor.Features.Game.Player;
using TypingSurvivor.Features.Game.Level;
using TypingSurvivor.Features.Game.Level.Data;
using TypingSurvivor.Features.Game.Settings;
using TypingSurvivor.Features.Game.Level.Tiles;

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
    private Grid _grid;
    private IMapGenerator _mapGenerator;
    private IItemPlacementStrategy _itemPlacementStrategy;
    private ItemRegistry _itemRegistry;
    private GameConfig _gameConfig;

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
    private Dictionary<string, TileBase> _tileNameToTileMap;

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
    public void Initialize(IMapGenerator mapGenerator, IItemPlacementStrategy itemPlacementStrategy, ItemRegistry itemRegistry, Grid grid, GameConfig gameConfig)
    {
        _mapGenerator = mapGenerator;
        _itemPlacementStrategy = itemPlacementStrategy;
        _itemRegistry = itemRegistry;
        _grid = grid;
        _gameConfig = gameConfig;

        // --- Build the comprehensive Tile ID Map from all sources ---
        _tileIdMap = new List<TileBase>();
        _tileToBaseIdMap = new Dictionary<TileBase, int>();
        
        var allTiles = new List<TileBase>();
        if(_gameConfig.WorldTiles != null) allTiles.AddRange(_gameConfig.WorldTiles);
        if(_itemRegistry != null)
        {
            if (_itemRegistry.AllItems != null) allTiles.AddRange(_itemRegistry.AllItems.Select(item => item.itemTile));
            if (_itemRegistry.AllEffectTiles != null) allTiles.AddRange(_itemRegistry.AllEffectTiles);
        }

        foreach (var tile in allTiles.Distinct().Where(t => t != null))
        {
            if (!_tileToBaseIdMap.ContainsKey(tile))
            {
                _tileToBaseIdMap[tile] = _tileIdMap.Count;
                _tileIdMap.Add(tile);
            }
        }

        // --- Build the TileName-to-TileBase dictionary for the map generator ---
        _tileNameToTileMap = new Dictionary<string, TileBase>();
        if (_gameConfig.WorldTiles != null)
        {
            foreach (var tile in _gameConfig.WorldTiles)
            {
                if (tile != null && !_tileNameToTileMap.ContainsKey(tile.name))
                {
                    _tileNameToTileMap.Add(tile.name, tile);
                }
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

        if (_itemPlacementStrategy != null)
        {
            _itemPlacementStrategy.Initialize(_itemRegistry);
        }
        else
        {
            Debug.LogError("ItemPlacementStrategy is null. Cannot generate items.");
        }

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

        foreach (var area in request.SpawnAreas)
        {
            if (area.MapGenerator == null)
            {
                Debug.LogError("A SpawnArea in the MapGenerationRequest has a null MapGenerator. Skipping this area.");
                continue;
            }
            var generatedBlocks = area.MapGenerator.Generate(_mapSeed, area.WorldOffset, _tileToBaseIdMap, _tileNameToTileMap);
            var generatedItems = _itemPlacementStrategy.PlaceItems(generatedBlocks, _itemRegistry, prng, _tileToBaseIdMap, area.WorldOffset);

            // Ensure items override blocks by removing any block at the same position as an item.
            var itemPositions = new HashSet<Vector3Int>(generatedItems.Select(item => item.Position));
            generatedBlocks.RemoveAll(block => itemPositions.Contains(block.Position));

            ChunkAndStoreMapData(generatedBlocks, _entireBlockMapData_Server);
            ChunkAndStoreMapData(generatedItems, _entireItemMapData_Server);

            // Ensure a safe spawn point by clearing the center of the area after generation.
            ClearArea(new Vector3Int(area.WorldOffset.x, area.WorldOffset.y, 0), 1); // Clears a 3x3 area
        }
        
        UpdateActiveChunks();
    }

    public List<Vector3Int> GetSpawnPoints(SpawnArea spawnArea)
    {
        if (!IsServer) return new List<Vector3Int>();

        var areaWalkableTiles = new List<Vector3Int>();
        var areaBounds = new BoundsInt();
        bool firstTile = true;

        foreach (var tileData in _entireBlockMapData_Server.SelectMany(kvp => kvp.Value))
        {
            if (Vector2.Distance(new Vector2(tileData.Position.x, tileData.Position.y), spawnArea.WorldOffset) < 100)
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
        if (_entireBlockMapData_Server.TryGetValue(chunkPos, out var blockTiles))
        {
            foreach (var tileData in blockTiles)
            {
                if (tileData.Position == gridPosition) return _tileIdMap[tileData.TileId];
            }
        }
        if (_entireItemMapData_Server.TryGetValue(chunkPos, out var itemTiles))
        {
            foreach (var tileData in itemTiles)
            {
                if (tileData.Position == gridPosition) return _tileIdMap[tileData.TileId];
            }
        }
        return null;
    }

    public void DestroyConnectedBlocks(ulong clientId, Vector3Int gridPosition)
    {
        if (!IsServer) return;
        var originalTile = GetTile(gridPosition);
        if (originalTile == null) return;
        var queue = new Queue<Vector3Int>();
        var visited = new HashSet<Vector3Int>();
        queue.Enqueue(gridPosition);
        visited.Add(gridPosition);
        while (queue.Count > 0)
        {
            var currentPos = queue.Dequeue();
            RemoveBlockDataAt(currentPos, clientId);
            var neighbors = new Vector3Int[] { currentPos + Vector3Int.up, currentPos + Vector3Int.down, currentPos + Vector3Int.left, currentPos + Vector3Int.right };
            foreach (var neighborPos in neighbors)
            {
                if (!visited.Contains(neighborPos) && GetTile(neighborPos) == originalTile)
                {
                    visited.Add(neighborPos);
                    queue.Enqueue(neighborPos);
                }
            }
        }
    }

    public void DestroyBlockAt(ulong clientId, Vector3Int gridPosition)
    {
        if (!IsServer) return;
        RemoveBlockDataAt(gridPosition, clientId);
    }

    private void RemoveBlockDataAt(Vector3Int gridPosition, ulong clientId)
    {
        var tile = GetTile(gridPosition);
        if (tile is IndestructibleTile) return;

        Vector2Int chunkPos = WorldToChunkPos(gridPosition);
        if (_entireBlockMapData_Server.TryGetValue(chunkPos, out var tiles))
        {
            tiles.RemoveAll(t => t.Position == gridPosition);
        }
        for (int i = _activeBlockTiles.Count - 1; i >= 0; i--)
        {
            if (_activeBlockTiles[i].Position == gridPosition)
            {
                _activeBlockTiles.RemoveAt(i);
                break;
            }
        }
        OnBlockDestroyed_Server?.Invoke(clientId, gridPosition);
    }

    public void PlaceBlock(Vector3Int gridPosition, TileBase tile)
    {
        if (!IsServer) return;
        if (tile == null) return;

        if (!_tileToBaseIdMap.TryGetValue(tile, out int tileId))
        {
            tileId = _tileIdMap.Count;
            _tileIdMap.Add(tile);
            _tileToBaseIdMap.Add(tile, tileId);
        }

        var tileData = new TileData { Position = gridPosition, TileId = tileId };
        Vector2Int chunkPos = WorldToChunkPos(gridPosition);

        if (!_entireBlockMapData_Server.ContainsKey(chunkPos))
        {
            _entireBlockMapData_Server[chunkPos] = new List<TileData>();
        }
        _entireBlockMapData_Server[chunkPos].RemoveAll(t => t.Position == gridPosition);
        _entireBlockMapData_Server[chunkPos].Add(tileData);

        for (int i = _activeBlockTiles.Count - 1; i >= 0; i--)
        {
            if (_activeBlockTiles[i].Position == gridPosition)
            {
                _activeBlockTiles.RemoveAt(i);
                break;
            }
        }
        _activeBlockTiles.Add(tileData);
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

    public TileInteractionType GetInteractionType(Vector3Int gridPosition)
    {
        if (!IsServer) return TileInteractionType.Walkable;

        var tile = GetTile(gridPosition);

        if (tile == null)
        {
            return TileInteractionType.Walkable;
        }
        if (tile is IndestructibleTile)
        {
            return TileInteractionType.Indestructible;
        }
        
        // If a tile exists and it's not indestructible, it must be a destructible block.
        return TileInteractionType.Destructible;
    }

    public bool IsWalkable(Vector3Int gridPosition)
    {
        if (!IsServer) return true;
        // This now correctly reflects that only walkable tiles should return true.
        return GetInteractionType(gridPosition) == TileInteractionType.Walkable;
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
                    DestroyBlockAt(0, targetPos);
                }
            }
        }
    }

    public void ForceChunkUpdateForPlayer(ulong clientId, Vector3 newPosition)
    {
        if (!IsServer) return;
        HandlePlayerMoved(clientId, newPosition);
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
        Vector3Int gridPos = _grid.WorldToCell(worldPos);
        return new Vector2Int(Mathf.FloorToInt((float)gridPos.x / _chunkSize), Mathf.FloorToInt((float)gridPos.y / _chunkSize));
    }
    
    private Vector2Int WorldToChunkPos(Vector3Int gridPos)
    {
        return new Vector2Int(Mathf.FloorToInt((float)gridPos.x / _chunkSize), Mathf.FloorToInt((float)gridPos.y / _chunkSize));
    }

    #endregion
}
