# **タイルとのインタラクション設計**

このドキュメントは、プレイヤーがマップ上のタイル（特にブロックタイル）とどのように相互作用（インタラクション）するかに関する現在の設計を定義します。

## 1. 設計概要: 「インタラクション種別」モデル

プレイヤーとタイルの相互作用は、タイルの性質を表す **`enum`（インタラクション種別）** に基づいて処理されます。これにより、単に通行可能か否かだけでなく、タイルの種類に応じた多様なフィードバックをプレイヤーに提供し、将来的な拡張（ダメージ床など）にも柔軟に対応できる設計となっています。

### 1.1. `TileInteractionType` enum

タイルの性質を定義する`enum`です。

```csharp
public enum TileInteractionType
{
    Walkable,         // 通行可能（何もない空間）
    Destructible,     // 破壊可能（通常のブロック）
    Indestructible    // 破壊不能（岩盤、Unchiなど）
}
```

### 1.2. `ILevelService`による情報提供

`ILevelService`は、タイルの性質を問い合わせるための統一された窓口を提供します。

-   `TileInteractionType GetInteractionType(Vector3Int gridPosition)`: 指定された座標のタイルの`TileInteractionType`を返します。

### 1.3. `PlayerFacade`によるロジックの実行

`PlayerFacade`のサーバーサイド移動ロジックは、`GetInteractionType`から返された`enum`の値に応じて、`switch`文で振る舞いを決定します。

```csharp
// PlayerFacade.cs の移動ロジック
var interactionType = _levelService.GetInteractionType(targetGridPos);

switch (interactionType)
{
    case TileInteractionType.Walkable:
        // 通常の移動処理を実行
        break;
    case TileInteractionType.Destructible:
        // タイピングモードへ移行
        break;
    case TileInteractionType.Indestructible:
        // 移動を中断し、その場に留まる
        // TODO: その場で「弾かれた」フィードバックを再生する (効果音、エフェクト)
        break;
}
```

この設計により、プレイヤーへのフィードバックが豊かになり、将来的なタイルの種類の追加にも柔軟に対応できる、拡張性の高い基盤が確立されています。
