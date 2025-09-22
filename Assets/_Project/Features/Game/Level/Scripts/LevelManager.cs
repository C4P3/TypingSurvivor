using System;
using System.Collections.Generic;
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

    [Header("Tile Database")]
    [Tooltip("TileID (ListのIndex) と TileBase を紐付けるためのリスト")]
    [SerializeField] private List<TileBase> _tileIdMap;
    private Dictionary<TileBase, int> _tileToBaseIdMap;

    private IMapGenerator _mapGenerator; // DIで注入


    // 同期されるタイルマップのデータ。
    // フィールド宣言時に readonly と new() を使って初期化するのがモダンな作法。
    private readonly NetworkList<TileData> _blockTiles = new();
    private readonly NetworkList<TileData> _itemTiles = new();

    public event Action<ulong, Vector3Int> OnBlockDestroyed_Server; // どのプレイヤーが、どの座標を破壊したか


    #region Unity Lifecycle & Netcode Callbacks

    public override void OnNetworkSpawn()
    {
        // サーバーはマップを生成する
        if (IsServer)
        {
            GenerateMap();
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
    }

    #endregion


    #region ILevelService (Server-side Logic)

    public void DestroyBlock(Vector3Int gridPosition, ulong clientId)
    {
        if (!IsServer) return; // このメソッドはサーバーでのみ実行可能

        // _blockTilesリストから該当する座標のTileDataを探して削除する
        for (int i = 0; i < _blockTiles.Count; i++)
        {
            if (_blockTiles[i].Position == gridPosition)
            {
                _blockTiles.RemoveAt(i);

                // TODO: ここでアイテムをドロップするロジックなどを追加

                break;
            }
        }
        
        // 処理が終わったら、イベントを発行して事実を報告する
        OnBlockDestroyed_Server?.Invoke(clientId, gridPosition);
    }

    public void RemoveItem(Vector3Int gridPosition)
    {
        // 実装は DestroyBlock と同様
        if (!IsServer) return;
    }
    
    /// <summary>
    /// [Server-Only] マップを生成するメソッド。
    /// サーバーでのみ実行されることを保証するために、冒頭でIsServerをチェックする。
    /// </summary>
    private void GenerateMap()
    {
        // このメソッドはサーバー以外では何もせず即座に終了する
        if (!IsServer) return;

        // TODO: ここにパーリンノイズなどを使ったマップ生成ロジックを実装
        // 生成したタイルを _blockTiles.Add(newTileData) のようにリストに追加していく
        // マップ生成は専門家にお願いするだけ
        var (generatedBlocks, generatedItems) = _mapGenerator.Generate(mapSeed);

        // 受け取った結果をNetworkListに流し込んで同期する
        foreach (var tile in generatedBlocks) _blockTiles.Add(tile);
    }

    #endregion


    #region Client-side View Updates

    private void OnBlockTilesChanged(NetworkListEvent<TileData> changeEvent)
    {
        switch (changeEvent.Type)
        {
            // リストに要素が追加された
            case NetworkListEvent<TileData>.EventType.Add:
                DrawTile(changeEvent.Value, _blockTilemap);
                break;

            // リストから要素が削除された
            case NetworkListEvent<TileData>.EventType.Remove:
                // changeEvent.Value は削除された値。これを使ってタイルを消す
                ClearTile(changeEvent.Value, _blockTilemap);
                break;

            // リスト全体がクリアされた
            case NetworkListEvent<TileData>.EventType.Clear:
                _blockTilemap.ClearAllTiles();
                break;

                // 他のイベントタイプも必要に応じて処理...
        }
    }
    
    private void OnItemTilesChanged(NetworkListEvent<TileData> changeEvent)
    {
        // _blockTiles と同様に実装
    }

    private void InitialDrawAllTiles()
    {
        foreach (var tileData in _blockTiles)
        {
            DrawTile(tileData, _blockTilemap);
        }
        foreach (var tileData in _itemTiles)
        {
            DrawTile(tileData, _itemTilemap);
        }
    }

    private void DrawTile(TileData data, Tilemap targetMap)
    {
        TileBase tileToDraw = _tileIdMap[data.TileId];
        targetMap.SetTile(data.Position, tileToDraw);
    }
    
    private void ClearTile(TileData data, Tilemap targetMap)
    {
        targetMap.SetTile(data.Position, null);
    }

    #endregion
}
