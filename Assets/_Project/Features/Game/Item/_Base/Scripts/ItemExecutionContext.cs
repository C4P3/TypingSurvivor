using UnityEngine;

/// <summary>
/// アイテム効果を実行するために必要な全ての情報を持つコンテキストオブジェクト。
/// ItemServiceがこのオブジェクトを構築し、IItemEffectに渡す。
/// </summary>
public class ItemExecutionContext
{
    public ulong UserId { get; } // 効果を発動したプレイヤーのID
    public Vector3Int ItemPosition { get; } // アイテムがあった座標

    // DIで注入される各種サービスへの参照
    public IGameStateWriter GameStateWriter { get; }
    public ILevelService LevelService { get; }
    public IPlayerStatusSystemWriter PlayerStatusSystem { get; } // プレイヤーの永続ステータスを管理する新しいシステム

    public ItemExecutionContext(
        ulong userId,
        Vector3Int itemPosition,
        IGameStateWriter gameStateWriter,
        ILevelService levelService,
        IPlayerStatusSystemWriter playerStatusSystem
    )
    {
        this.UserId = userId;
        this.ItemPosition = itemPosition;
        this.GameStateWriter = gameStateWriter;
        this.LevelService = levelService;
        this.PlayerStatusSystem = playerStatusSystem;
    }
}