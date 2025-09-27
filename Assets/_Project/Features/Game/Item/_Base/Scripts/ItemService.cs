using UnityEngine;
using TypingSurvivor.Features.Core.PlayerStatus;
using TypingSurvivor.Features.Game.Level;

public class ItemService : MonoBehaviour, IItemService
{
    // --- Dependencies (injected by Bootstrapper) ---
    private ItemRegistry _itemRegistry;
    private ILevelService _levelService;
    private IGameStateWriter _gameStateWriter;
    private IPlayerStatusSystemWriter _playerStatusSystemWriter;

    public void Initialize(ILevelService levelService, IGameStateWriter gameStateWriter, IPlayerStatusSystemWriter playerStatusSystemWriter, ItemRegistry itemRegistry)
    {
        _levelService = levelService;
        _gameStateWriter = gameStateWriter;
        _playerStatusSystemWriter = playerStatusSystemWriter;
        _itemRegistry = itemRegistry;
    }

    public void AcquireItem(ulong clientId, Vector3Int itemGridPosition)
    {
        if (_itemRegistry == null)
        {
            Debug.LogError("ItemRegistryが設定されていません。");
            return;
        }
        if (_levelService == null)
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

        // 4. ItemExecutionContextを構築
        var context = new ItemExecutionContext(
            clientId,
            itemGridPosition,
            _gameStateWriter,
            _levelService,
            _playerStatusSystemWriter
        );

        // 5. 全ての効果を実行
        foreach (var effect in itemData.Effects)
        {
            effect.Execute(context);
        }
        
        // TODO: アイテム取得エフェクトの再生をクライアントに通知する
    }
}