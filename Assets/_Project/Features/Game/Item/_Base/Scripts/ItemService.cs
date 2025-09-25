using UnityEngine;
using TypingSurvivor.Features.Core.App; // AppManager を使うために必要
using TypingSurvivor.Features.Core.PlayerStatus;

public class ItemService : MonoBehaviour, IItemService
{
    [SerializeField] private ItemRegistry _itemRegistry;

    // --- 依存サービスの参照 ---
    private ILevelService _levelService;
    private IGameStateWriter _gameStateWriter;
    private IPlayerStatusSystemWriter _playerStatusSystemWriter;

    private void Start()
    {
        var serviceLocator = AppManager.Instance;
        if (serviceLocator == null)
        {
            Debug.LogError("AppManager instance not found!");
            return;
        }
        
        // AppManagerから依存サービスを取得
        _levelService = serviceLocator.GetService<ILevelService>();
        _gameStateWriter = serviceLocator.GetService<IGameStateWriter>();
        _playerStatusSystemWriter = serviceLocator.StatusWriter; // Coreサービスは直接参照
    }

    public void AcquireItem(ulong clientId, Vector3Int itemGridPosition)
    {
        if (_itemRegistry == null)
        {
            Debug.LogError("ItemRegistryが設定されていません。");
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