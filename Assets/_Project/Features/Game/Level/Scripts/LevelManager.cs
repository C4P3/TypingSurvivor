using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Level (タイルマップ) の状態を管理し、変更ロジックを実行するクラス。
/// サーバーサイドで動作し、状態をクライアントに同期する。
/// </summary>
public class LevelManager : NetworkBehaviour, ILevelService
{
    [Header("References")]
    [SerializeField] private Tilemap _blockTilemap;
    [SerializeField] private Tilemap _itemTilemap;

    [Header("Dependencies")]
    [Tooltip("DIなどで注入されるマップ生成アルゴリズム")]
    [SerializeField] private ScriptableObject _mapGeneratorSO;
    private IMapGenerator _mapGenerator;

    [Header("Tile Database")]
    [Tooltip("TileID (ListのIndex) と TileBase を紐付けるためのリスト")]
    [SerializeField] private List<TileBase> _tileIdMap;
    private Dictionary<TileBase, int> _tileToBaseIdMap;

    [Header("Map Settings")]
    [SerializeField] private long _mapSeed;
    [SerializeField] private bool _useRandomSeed = true;

    [Header("Chunk System Settings")]
    [SerializeField] private int _chunkSize = 16;
    [Tooltip("プレイヤーの周囲何チャンク分を同期対象にするか")]
    [SerializeField] private int _generationRadiusInChunks = 2;

    // --- Server-Side Data ---
    private Dictionary<Vector2Int, List<TileData>> _entireBlockMapData_Server;
    private Dictionary<Vector2Int, List<TileData>> _entireItemMapData_Server;
    private readonly Dictionary<ulong, Vector2Int> _playerChunkPositions_Server = new();
    private readonly HashSet<Vector2Int> _activeChunks_Server = new();

    // --- Synced Data ---
    private readonly NetworkList<TileData> _activeBlockTiles = new();
    private readonly NetworkList<TileData> _activeItemTiles = new();


    #region Unity Lifecycle & Netcode Callbacks

    private void Awake()
    {
        // 依存性を解決
        _mapGenerator = _mapGeneratorSO as IMapGenerator;
        if (_mapGenerator == null)
        {
            Debug.LogError("IMapGeneratorがアタッチされていません。");
        }

        // TileBaseからIDを逆引きするための辞書を初期化
        _tileToBaseIdMap = new Dictionary<TileBase, int>();
        for (int i = 0; i < _tileIdMap.Count; i++)
        {
            if (_tileIdMap[i] != null)
            {
                _tileToBaseIdMap[_tileIdMap[i]] = i;
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            GenerateAndChunkMap();

            // プレイヤーのスポーン/デスポーンイベントを購読する
            // ★TODO: このイベントはPlayerFacade側で定義・発行する必要があります
            // PlayerFacade.OnPlayerSpawned_Server += HandlePlayerSpawned;
            // PlayerFacade.OnPlayerDespawned_Server += HandlePlayerDespawned;
            // PlayerFacade.OnPlayerMoved_Server += HandlePlayerMoved;

            // 既に接続しているクライアントを処理
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
            // ★TODO: PlayerFacadeのイベント購読解除
            // PlayerFacade.OnPlayerSpawned_Server -= HandlePlayerSpawned;
            // PlayerFacade.OnPlayerDespawned_Server -= HandlePlayerDespawned;
            // PlayerFacade.OnPlayerMoved_Server -= HandlePlayerMoved;
        }
    }
    #endregion

    #region Event Handlers (Called from Player System)

    /// <summary>
    /// [Server-Only] プレイヤーが移動した時にPlayerFacadeから呼び出される
    /// </summary>
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

    #endregion

    #region ILevelService (Server-side Logic)

    public void DestroyBlock(ulong clientId, Vector3Int gridPosition)
    {
        if (!IsServer) return;

        Vector2Int chunkPos = WorldToChunkPos(gridPosition);
        if (_entireBlockMapData_Server.TryGetValue(chunkPos, out var tiles))
        {
            tiles.RemoveAll(t => t.Position == gridPosition);
        }

        // TileIdが不明なので、座標だけで検索して削除する
        // 効率は良くないが、構造上やむを得ない
        for(int i = _activeBlockTiles.Count - 1; i >= 0; i--)
        {
            if(_activeBlockTiles[i].Position == gridPosition)
            {
                _activeBlockTiles.RemoveAt(i);
                break;
            }
        }
        
        // ILevelService.OnBlockDestroyed_Server.Invoke(clientId, gridPosition);
    }

    public void RemoveItem(ulong clientId, Vector3Int gridPosition)
    {
        if (!IsServer) return; // サーバーでのみ実行

        // 全マップデータからタイルを削除する
        Vector2Int chunkPos = WorldToChunkPos(gridPosition);
        if (_entireItemMapData_Server.TryGetValue(chunkPos, out var tiles))
        {
            tiles.RemoveAll(t => t.Position == gridPosition);
        }

        // _itemTilesリストから該当する座標のTileDataを探して削除する
        for (int i = 0; i < _activeItemTiles.Count; i++)
        {
            if (_activeItemTiles[i].Position == gridPosition)
            {
                _activeItemTiles.RemoveAt(i);
                break;
            }
        }
    }
    #endregion


    #region Server-Side Chunk Management

    /// <summary>
    /// [Server-Only] マップを生成し、チャンクごとに分割して保存する。
    /// </summary>
    private void GenerateAndChunkMap()
    {
        if (!IsServer) return;

        _entireBlockMapData_Server = new Dictionary<Vector2Int, List<TileData>>();
        _entireItemMapData_Server = new Dictionary<Vector2Int, List<TileData>>();

        if (_mapGenerator == null)
        {
            Debug.LogError("MapGeneratorが設定されていません。");
            return;
        }

        if (_useRandomSeed) _mapSeed = System.DateTime.Now.Ticks;

        var (generatedBlocks, generatedItems) = _mapGenerator.Generate(_mapSeed);

        // ブロックタイルをチャンク分けする
        foreach (var tile in generatedBlocks)
        {
            Vector2Int chunkPos = WorldToChunkPos(tile.Position);
            if (!_entireBlockMapData_Server.ContainsKey(chunkPos))
            {
                _entireBlockMapData_Server[chunkPos] = new List<TileData>();
            }
            _entireBlockMapData_Server[chunkPos].Add(tile);
        }
        // アイテムタイルをチャンク分けする
        foreach (var tile in generatedItems)
        {
            Vector2Int chunkPos = WorldToChunkPos(tile.Position);
            if (!_entireItemMapData_Server.ContainsKey(chunkPos))
            {
                _entireItemMapData_Server[chunkPos] = new List<TileData>();
            }
            _entireItemMapData_Server[chunkPos].Add(tile);
        }
    }

    /// <summary>
    /// [Server-Only] プレイヤーがチャンクをまたいでいないかチェックする。
    /// </summary>
    private void CheckForChunkUpdates()
    {
        bool needsUpdate = false;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;

            Vector3 playerPos = client.PlayerObject.transform.position;
            Vector2Int currentChunk = WorldToChunkPos(playerPos);

            if (_playerChunkPositions_Server.TryGetValue(client.ClientId, out var previousChunk))
            {
                if (previousChunk != currentChunk)
                {
                    _playerChunkPositions_Server[client.ClientId] = currentChunk;
                    needsUpdate = true;
                }
            }
        }
        if (needsUpdate)
        {
            UpdateActiveChunks();
        }
    }

    /// <summary>
    /// [Server-Only] 全プレイヤーにとって必要なチャンクを計算し、ロード/アンロードを実行する。
    /// </summary>
    private void UpdateActiveChunks()
    {
        // 1. 全プレイヤーにとって「見えるべき」チャンクのリストを計算する
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

        // 2. 不要になったチャンクをアンロードする
        var chunksToUnload = _activeChunks_Server.Except(allRequiredChunks).ToList();
        foreach (var chunkPos in chunksToUnload)
        {
            UnloadChunk(chunkPos);
            _activeChunks_Server.Remove(chunkPos);
        }

        // 3. 新しく必要になったチャンクをロードする
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
    
    // NetworkList<TileData> からタイルをアンロードする共通メソッド
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

    #region Connection Handling

    private void HandleClientConnected(ulong clientId)
    {
        // プレイヤーの初期チャンク位置を登録する (PlayerObjectのスポーンを待つのが理想)
        _playerChunkPositions_Server[clientId] = Vector2Int.zero;
        UpdateActiveChunks();
    }
    
    // TODO: PlayerFacadeがスポーンした時に呼び出されるメソッド
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
        else
        {
            Debug.LogWarning($"不正なTileId: {data.TileId} が座標 {data.Position} で検出されました。");
        }
    }
    
    private void ClearTile(TileData data, Tilemap targetMap)
    {
        targetMap.SetTile(data.Position, null);
    }
    
    private Vector2Int WorldToChunkPos(Vector3 worldPos)
    {
        Vector3Int gridPos = _blockTilemap.WorldToCell(worldPos);
        int x = Mathf.FloorToInt((float)gridPos.x / _chunkSize);
        int y = Mathf.FloorToInt((float)gridPos.y / _chunkSize);
        return new Vector2Int(x, y);
    }
    
    private Vector2Int WorldToChunkPos(Vector3Int gridPos)
    {
        int x = Mathf.FloorToInt((float)gridPos.x / _chunkSize);
        int y = Mathf.FloorToInt((float)gridPos.y / _chunkSize);
        return new Vector2Int(x, y);
    }

    #endregion
}