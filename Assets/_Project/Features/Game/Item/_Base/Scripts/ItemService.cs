using UnityEngine;
using TypingSurvivor.Features.Core.PlayerStatus;
using TypingSurvivor.Features.Game.Level;
using System.Collections.Generic;

using TypingSurvivor.Features.Core.Audio;
using TypingSurvivor.Features.Core.VFX;

public class ItemService : MonoBehaviour, IItemService
{
    // --- Dependencies (injected by Bootstrapper) ---
    private ItemRegistry _itemRegistry;
    private ILevelService _levelService;
    private IGameStateReader _gameStateReader;
    private IGameStateWriter _gameStateWriter;
    private IPlayerStatusSystemWriter _playerStatusSystemWriter;
    private SfxManager _sfxManager;
    private EffectManager _effectManager;
    private Grid _grid;

    public void Initialize(ILevelService levelService, IGameStateReader gameStateReader, IGameStateWriter gameStateWriter, IPlayerStatusSystemWriter playerStatusSystemWriter, ItemRegistry itemRegistry, SfxManager sfxManager, EffectManager effectManager, Grid grid)
    {
        _levelService = levelService;
        _gameStateReader = gameStateReader;
        _gameStateWriter = gameStateWriter;
        _playerStatusSystemWriter = playerStatusSystemWriter;
        _itemRegistry = itemRegistry;
        _effectManager = effectManager;
        _sfxManager = sfxManager;
        _grid = grid;
    }

    public void AcquireItem(ulong clientId, Vector3Int itemGridPosition, Vector3Int lastMoveDirection)
    {
        if (_itemRegistry == null)
        {
            Debug.LogError("ItemRegistryが設定されていません。");
            return;
        }
        if (_levelService == null || _gameStateReader == null || _grid == null)
        {
            Debug.LogError("ItemService has not been initialized correctly.");
            return;
        }

        // 1. 座標からタイルを取得
        var tile = _levelService.GetTile(itemGridPosition);
        if (tile == null) return;

        // 2. タイルからItemDataを取得
        var itemData = _itemRegistry.GetItemData(tile);
        if (itemData == null) return;

        // 3. アイテムをマップから削除するようLevelServiceに依頼
        _levelService.RemoveItem(itemGridPosition);

        // 4. Find opponents
        var opponentClientIds = new List<ulong>();
        foreach (var playerData in _gameStateReader.PlayerDatas)
        {
            if (playerData.ClientId != clientId)
            {
                opponentClientIds.Add(playerData.ClientId);
            }
        }

        // 5. ItemExecutionContextを構築
        var worldPosition = _grid.GetCellCenterWorld(itemGridPosition);
        var context = new ItemExecutionContext(
            clientId,
            itemGridPosition,
            worldPosition,
            lastMoveDirection,
            opponentClientIds,
            _gameStateReader,
            _gameStateWriter,
            _levelService,
            _playerStatusSystemWriter,
            _sfxManager,
            _effectManager
        );

        // 6. 全ての効果を実行
        foreach (var effect in itemData.Effects)
        {
            effect.Execute(context);
        }
        
        // TODO: アイテム取得エフェクトの再生をクライアントに通知する
    }
}