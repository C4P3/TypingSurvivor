# **PlayerStatusSystem 設計ドキュメント**

## 1. 責務

`PlayerStatusSystem` は、ゲーム内に存在する全プレイヤーの基本ステータスと一時的なステータス補正（Modifier）を一元管理する、**サーバーサイドのコアサービス**です。

-   **一元管理**: `MoveSpeed` や `MaxOxygen` といったプレイヤーのステータスに関する唯一の信頼できる情報源（Source of Truth）として機能します。
-   **補正の適用**: 永続的なアップグレードと、アイテム効果などによる一時的なステータス変化の両方を管理します。
-   **疎結合**: ステータスを変更する機能（例: `ItemService`）と、ステータスを利用する機能（例: `PlayerFacade`）を分離（デカップリング）します。

## 2. 配置場所

-   **パス**: `Assets/_Project/Features/Core/PlayerStatus/`
-   **理由**: このシステムは特定の `Player` `GameObject` に紐づくものではなく、全プレイヤーのデータを管理するグローバルなサービスです。`Core` 機能群に配置することで、その役割がプロジェクトの基盤となるサービスであることを明確にします。

## 3. 主要コンポーネント

### 3.1. `PlayerStatusSystem.cs`

`IPlayerStatusSystemReader` と `IPlayerStatusSystemWriter` を実装する、プレーンなC#クラスです。サーバー起動時に `AppManager` によって唯一のインスタンスが生成されます。

-   **責務**:
    -   プレイヤーごと (`clientId`) のステータス情報を辞書として保持します。
    -   有効な `StatModifier`（永続・一時両方）のリストを管理します。
    -   基本値と、有効な全ての補正を合算し、最終的なステータス値を計算します。
    -   一時的な補正のライフサイクルを管理します（持続時間が経過したものをリストから削除するなど）。

### 3.2. `IPlayerStatusSystemReader.cs` (クエリ・インターフェース)

プレイヤーのステータスへの読み取り専用アクセスを提供します。

-   **メソッド**:
    -   `float GetStatValue(ulong clientId, PlayerStat stat)`: 特定プレイヤーの、最終的に計算されたステータス値を取得します。

### 3.3. `IPlayerStatusSystemWriter.cs` (コマンド・インターフェース)

プレイヤーのステータスを変更するための書き込みアクセスを提供します。

-   **メソッド**:
    -   `void ApplyModifier(ulong clientId, StatModifier modifier)`: プレイヤーのステータスに対し、一時的または永続的な補正を適用します。

### 3.4. `StatModifier.cs` (データ構造)

ステータスに対する単一の補正内容を定義する構造体またはクラスです。

-   **プロパティ**:
    -   `PlayerStat Stat`: 補正対象のステータス。
    -   `float Value`: 補正値。
    -   `ModifierType Type`: 補正の種類（例: `Additive` (加算), `Multiplicative` (乗算)）。
    -   `float Duration`: 効果の持続時間（秒）。`0` 以下は永続的な補正とみなします。

### 3.5. `PlayerDefaultStats.cs` (ScriptableObject)

-   **配置場所**: `Assets/_Project/Settings/`
-   **責務**: 全てのプレイヤーの基本ステータス値を保持します。
-   **利用方法**: `PlayerStatusSystem` は `GameConfig` を通じてこのアセットへの参照を受け取り、基本値を取得します。これにより、コード内にハードコードされた数値をなくします。

## 4. データフローの例：「スピードアップ」アイテム

1.  **取得**: `PlayerFacade` (サーバー) がアイテムタイルに接触し、`IItemService.AcquireItem(clientId, itemId)` を呼び出します。
2.  **効果実行**: `ItemService` は「スピードアップ」アイテムとその効果クラス `MoveSpeedupEffect` を特定します。
3.  **補正生成**: `MoveSpeedupEffect` が新しい `StatModifier` を生成します。
    ```csharp
    var modifier = new StatModifier
    {
        Stat = PlayerStat.MoveSpeed,
        Value = 1.5f,
        Type = ModifierType.Multiplicative,
        Duration = 10.0f
    };
    ```
4.  **コマンド発行**: `ItemService` は `AppManager` から `IPlayerStatusSystemWriter` を取得し、`writer.ApplyModifier(clientId, modifier)` を呼び出します。
5.  **状態変更**: `PlayerStatusSystem` は、指定されたプレイヤーのアクティブな補正リストに、この `modifier` を追加します。
6.  **クエリ発行**: その後、`PlayerFacade` の `ContinuousMove_Server` コルーチンが実行される際、`AppManager` から `IPlayerStatusSystemReader` を取得します。
7.  **値の計算**: `reader.GetStatValue(clientId, PlayerStat.MoveSpeed)` を呼び出します。`PlayerStatusSystem` は `(基本速度 + 永続ボーナス) * 1.5` のように最終的な速度を計算し、その結果を返します。
8.  **効果の削除**: `PlayerStatusSystem` は、この補正の持続時間を管理し、10秒経過後にアクティブなリストから削除する責務を持ちます。

この設計は、プロジェクトのコア原則である **関心の分離** と **CQRS (コマンド・クエリ責務分離)** を遵守するものです。