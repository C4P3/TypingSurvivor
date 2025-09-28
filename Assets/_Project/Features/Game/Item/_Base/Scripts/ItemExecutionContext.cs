using UnityEngine;
using TypingSurvivor.Features.Core.PlayerStatus;
using TypingSurvivor.Features.Game.Level;
using System.Collections.Generic;

/// <summary>
/// アイテム効果を実行するために必要な全ての情報を持つコンテキストオブジェクト。
/// ItemServiceがこのオブジェクトを構築し、IItemEffectに渡す。
/// </summary>
public class ItemExecutionContext
{
    public ulong UserId { get; } // 効果を発動したプレイヤーのID
    public Vector3Int ItemPosition { get; } // アイテムがあった座標
    public Vector3Int UserLastMoveDirection { get; } // Rocket用: アイテムを拾ったプレイヤーの最後の移動方向
    public IReadOnlyList<ulong> OpponentClientIds { get; } // 妨害系用: 相手プレイヤーのIDリスト

    // DIで注入される各種サービスへの参照
    public IGameStateWriter GameStateWriter { get; }
    public ILevelService LevelService { get; }
    public IPlayerStatusSystemWriter PlayerStatusSystem { get; } // プレイヤーの永続ステータスを管理する新しいシステム

    public ItemExecutionContext(
        ulong userId,
        Vector3Int itemPosition,
        Vector3Int userLastMoveDirection,
        IReadOnlyList<ulong> opponentClientIds,
        IGameStateWriter gameStateWriter,
        ILevelService levelService,
        IPlayerStatusSystemWriter playerStatusSystem
    )
    {
        UserId = userId;
        ItemPosition = itemPosition;
        UserLastMoveDirection = userLastMoveDirection;
        OpponentClientIds = opponentClientIds;
        GameStateWriter = gameStateWriter;
        LevelService = levelService;
        PlayerStatusSystem = playerStatusSystem;
    }
}