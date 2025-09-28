# **タイルとのインタラクション設計**

このドキュメントは、プレイヤーがマップ上のタイル（特にブロックタイル）とどのように相互作用（インタラクション）するかに関する、現在および将来の設計方針を定義する。

## 1. 現状の実装 (v1)

現在のシステムでは、プレイヤーが移動先にブロックタイルを発見した場合、`ILevelService.IsWalkable()`を通じて、そのタイルが「歩けるか（`false`）」、あるいは「歩けないか（`true`）」という二値的な情報のみを取得している。

このシンプルな設計は、破壊可能なブロックに対してタイピングモードを開始するという基本的な要件を満たしている。

## 2. 課題と将来の構想

`IsWalkable()`だけでは、プレイヤーへのフィードバックが不十分になるケースが存在する。

*   **破壊不能ブロック:** 現在の実装では、プレイヤーは破壊不能ブロック（`IndestructibleTile`）に対してもタイピングモードを開始しようとしてしまう。これはプレイヤーに「なぜこのブロックは壊せないんだ？」という混乱を与える。
*   **将来的な拡張:** 「踏むとダメージを受ける溶岩タイル」や「滑る氷のタイル」のような、新しい種類のタイルを追加する際に、`bool`値だけではタイルの多様な性質を表現できない。

この課題を解決し、よりリッチで直感的なプレイヤー体験を実現するため、以下の将来構想を計画する。

## 3. 将来の設計案 (v2): 「インタラクション種別」の導入

プレイヤーとタイルの相互作用を、単なる`bool`値ではなく、タイルの性質を表す**`enum`（インタラクション種別）**に基づいて行うようにリファクタリングする。

### 3.1. `TileInteractionType` enum

タイルの性質を定義する`enum`を新設する。

```csharp
public enum TileInteractionType
{
    Walkable,         // 通行可能（何もない空間）
    Destructible,     // 破壊可能（通常のブロック）
    Indestructible    // 破壊不能（岩盤、Unchiなど）
    // Damaging,      // 将来的な拡張: ダメージタイル
    // Slippery,      // 将来的な拡張: 滑るタイル
}
```

### 3.2. ILevelServiceの拡張

`ILevelService`に、`bool IsWalkable(...)`を置き換える新しいメソッドを追加する。

*   `TileInteractionType GetInteractionType(Vector3Int gridPosition)`

### 3.3. PlayerFacadeのロジック刷新

`PlayerFacade`の移動ロジックは、`GetInteractionType`から返された`enum`の値に応じて、`switch`文で振る舞いを決定するようになる。

```csharp
// PlayerFacade.cs の将来の移動ロジック (擬似コード)
var interactionType = _levelService.GetInteractionType(targetGridPos);

switch (interactionType)
{
    case TileInteractionType.Walkable:
        // 通常の移動処理
        break;
    case TileInteractionType.Destructible:
        // タイピングモードへ移行
        break;
    case TileInteractionType.Indestructible:
        // 移動せず、その場で「弾かれた」フィードバックを再生する (効果音、エフェクト)
        PlayCollisionFeedbackClientRpc(...);
        break;
}
```

この設計への移行により、プレイヤーへのフィードバックが豊かになり、将来的なタイルの種類の追加にも柔軟に対応できる、拡張性の高い基盤が確立される。
