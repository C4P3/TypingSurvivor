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

    private IMapGenerator _mapGenerator;// DIで注入
    private event Action<ulong, Vector3Int> OnBlockDestroyed_Server;


    [Header("Tile Database")]
    [Tooltip("TileID (ListのIndex) と TileBase を紐付けるためのリスト")]
    [SerializeField] private List<TileBase> _tileIdMap;
    // TileBaseからIDを逆引きするための辞書。コード上で自動生成します。
    private Dictionary<TileBase, int> _tileToBaseIdMap;

    [Header("Map Settings")]
    [SerializeField] private long _mapSeed;
    [SerializeField] private bool _useRandomSeed = true;

    [Header("Chunk System Settings")]
    [SerializeField] private int _chunkSize = 16;
    [Tooltip("プレイヤーの周囲何チャンク分を同期対象にするか")]
    [SerializeField] private int _generationRadiusInChunks = 2;
    [Tooltip("プレイヤーのチャンク位置をチェックする頻度(秒)")]
    [SerializeField] private float _chunkCheckInterval = 1.0f;
    private float _chunkCheckTimer = 0f;

    // [サーバーのみ] マップジェネレーターが生成した全タイルデータ(ブロック、アイテム)
    private Dictionary<Vector2Int, List<TileData>> _entireBlockMapData_Server;
    private Dictionary<Vector2Int, List<TileData>> _entireItemMapData_Server;
    // [サーバーのみ] 各プレイヤーがどのチャンクにいるかを管理
    private readonly Dictionary<ulong, Vector2Int> _playerChunkPositions_Server = new();
    // [サーバーのみ] 現在 NetworkList にロードされているチャンクの座標
    private readonly HashSet<Vector2Int> _activeChunks_Server = new();

    // [同期対象] 全プレイヤーの視界内にあるアクティブなチャンクのタイルデータ
    private readonly NetworkList<TileData> _blockTiles = new();
    private readonly NetworkList<TileData> _itemTiles = new();


    #region Unity Lifecycle & Netcode Callbacks

    private void Awake()
    {
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
        // サーバーはマップを生成する
        if (IsServer)
        {
            GenerateAndChunkMap();
            // サーバー起動時のコールバック設定
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;

            // 既に接続しているクライアントを処理
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                HandleClientConnected(client.ClientId);
            }
        }

        // クライアントは、NetworkListの変更を監視し、タイルマップの見た目を更新する
        if (IsClient)
        {
            _blockTiles.OnListChanged += OnBlockTilesChanged;
            _itemTiles.OnListChanged += OnItemTilesChanged;

            // 最初に接続した時点で、現在のリストを元にマップを描画する
            InitialDrawAllTiles();
        }
    }

    public override void OnNetworkDespawn()
    {
        // オブジェクトが破棄される際にイベントの購読を解除する（メモリリーク防止）
        if (IsClient)
        {
            _blockTiles.OnListChanged -= OnBlockTilesChanged;
            _itemTiles.OnListChanged -= OnItemTilesChanged;
        }

        // サーバーがシャットダウンする際にコールバックを解除
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
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

            if (_playerChunkPositions_Server.TryGetValue(client.ClientId, out Vector2Int previousChunk))
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
            foreach (var tile in blockTiles) _blockTiles.Add(tile);
        }
        if (_entireItemMapData_Server.TryGetValue(chunkPos, out var itemTiles))
        {
            foreach (var tile in itemTiles) _itemTiles.Add(tile);
        }
    }

    private void UnloadChunk(Vector2Int chunkPos)
    {
        void NetworkListTileDataRemoveAll(NetworkList<TileData> tileDataList, Vector2Int chunkPos, int chunkSize)
        {
            int startX = chunkPos.x * chunkSize;
            int startY = chunkPos.y * chunkSize;
            for (int i = tileDataList.Count - 1; i >= 0; i--)
            {
                var tile = tileDataList[i];
                if (tile.Position.x >= startX && tile.Position.x < startX + chunkSize &&
                    tile.Position.y >= startY && tile.Position.y < startY + chunkSize)
                {
                    tileDataList.RemoveAt(i);
                }
            }
        }
        NetworkListTileDataRemoveAll(_blockTiles, chunkPos, _chunkSize);
        NetworkListTileDataRemoveAll(_blockTiles, chunkPos, _chunkSize);
    }

    #endregion

    #region Connection Handling
    
    private void HandleClientConnected(ulong clientId)
    {
        // PlayerObjectがまだスポーンしていない可能性があるので、nullチェック
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
        {
            Vector3 playerPos = client.PlayerObject.transform.position;
            _playerChunkPositions_Server[clientId] = WorldToChunkPos(playerPos);
            UpdateActiveChunks();
        }
        else
        {
            // PlayerObjectがスポーンするまで待つか、初期位置を別の方法で取得する必要がある
            _playerChunkPositions_Server[clientId] = Vector2Int.zero;
            UpdateActiveChunks();
        }
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        if (_playerChunkPositions_Server.ContainsKey(clientId))
        {
            _playerChunkPositions_Server.Remove(clientId);
            UpdateActiveChunks();
        }
    }
    
    #endregion

    #region ILevelService (Server-side Logic)

    public void DestroyBlock(ulong clientId, Vector3Int gridPosition)
    {
        if (!IsServer) return;

        // 全マップデータからタイルを削除する
        Vector2Int chunkPos = WorldToChunkPos(gridPosition);
        if (_entireBlockMapData_Server.TryGetValue(chunkPos, out var tiles))
        {
            tiles.RemoveAll(t => t.Position == gridPosition);
        }
        
        // NetworkListからもタイルを削除する
        for (int i = 0; i < _blockTiles.Count; i++)
        {
            if (_blockTiles[i].Position == gridPosition)
            {
                _blockTiles.RemoveAt(i);
                break;
            }
        }

        // 処理が終わったら、イベントを発行して事実を報告する
        OnBlockDestroyed_Server?.Invoke(clientId, gridPosition);
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
        for (int i = 0; i < _itemTiles.Count; i++)
        {
            if (_itemTiles[i].Position == gridPosition)
            {
                _itemTiles.RemoveAt(i);
                break;
            }
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
        foreach (var tileData in _blockTiles) DrawTile(tileData, _blockTilemap);
        foreach (var tileData in _itemTiles) DrawTile(tileData, _itemTilemap);
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