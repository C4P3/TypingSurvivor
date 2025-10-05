# **Level機能 設計ドキュメント**

## **1. 責務**

Level機能は、ゲームの舞台となる**世界の物理的な状態**を管理します。プレイヤーやアイテムが存在する「空間」そのものに対する全ての責務を持ちます。

*   `GameManager`から渡される**「マップ生成リクエスト」**に基づき、地形生成とアイテム配置を組み合わせてワールドを構築する。
*   広大な、あるいは複数の離れた領域から成るマップを効率的に扱うため、プレイヤーの周囲の**チャンク**のみをクライアントと同期する。
*   外部からの要求（`DestroyBlock`など）に応じてマップの状態を変更し、その結果を全クライアントに同期させる。

## **2. 設計思想: 「マップ生成リクエスト」モデル**

本プロジェクトのマップ生成は、将来の多様なゲームモード（シード値共有、協力プレイなど）に柔軟に対応するため、**「マップ生成リクエスト」**という設計思想に基づいています。

このモデルでは、`GameManager`（司令官）がマップ生成の詳細を知る必要はありません。代わりに、`GameManager`は現在のゲームモードに応じて**「どのようなワールドを作ってほしいか」という設計図 (`MapGenerationRequest`)** を作成し、それを`LevelManager`（建築家）に渡します。`LevelManager`は受け取った設計図通りにワールドを構築することにのみ責任を持ちます。

### **2.1. `MapGenerationRequest` モデル**

この設計の核となるデータ構造です。

```csharp
// ワールド全体の生成ルールを定義
public class MapGenerationRequest
{
    public long BaseSeed; // 0の場合は完全ランダム
    public List<SpawnArea> SpawnAreas; // 各プレイヤー/チームの領域定義
}

// 個別のスポーン領域の定義
public class SpawnArea
{
    public List<ulong> PlayerClientIds; // このエリアにスポーンするプレイヤー
    public Vector2Int WorldOffset;      // このエリアの中心となるワールド座標オフセット
    public IMapGenerator MapGenerator;  // このエリアの地形を生成するジェネレーター
    public ISpawnPointStrategy SpawnPointStrategy; // エリア内のスポーン地点を決める戦略
}
```

### **2.2. 多様なゲームモードの実現**

このリクエストモデルにより、`GameManager`がリクエストの作り方を変えるだけで、様々なルールを実装できます。

*   **通常対戦（ランダムシード、位置分離）**:
    *   `BaseSeed = 0` (ランダム)
    *   `SpawnAreas`に、プレイヤーA用 (`WorldOffset=(0,0)`) とプレイヤーB用 (`WorldOffset=(1000,0)`) の2つの`SpawnArea`を追加する。
*   **シード値共有マッチ（シード指定、位置分離）**:
    *   `BaseSeed`にルームIDなどから生成した値を設定。
    *   他は通常対戦と同じ。
*   **チーム協力モード（シード指定、位置共有）**:
    *   `BaseSeed`を指定。
    *   `SpawnAreas`に、プレイヤーAとBを**同じリスト**に含む単一の`SpawnArea` (`WorldOffset=(0,0)`) を追加する。`SpawnPointStrategy`には、味方同士が近くにスポーンする`CoopSpawnStrategy`を使用する。

## **3. 主要コンポーネント**

### **3.1. LevelManager.cs**

*   **役割**: `ILevelService`インターフェースを実装する、サーバーサイドのメインクラス。**マップ生成リクエストの実行者**。
*   **責務**:
    *   **初期化**: `GameSceneBootstrapper`から、現在のゲームモードに応じた`IMapGenerator`や`IItemPlacementStrategy`を受け取る。
    *   `GenerateWorld(MapGenerationRequest request)`: `GameManager`からリクエストを受け取り、その内容に従ってワールド全体を構築する。このメソッドの内部で、受け取っていた`IItemPlacementStrategy`を初期化し、アイテム配置に使用する。
    *   **状態の所有**: `NetworkList<TileData>`として、現在アクティブな（同期対象の）タイルデータを保持する。
    *   **チャンク管理**: 全プレイヤーの位置を監視し、どのチャンクをアクティブにすべきかを判断し、`NetworkList`の内容を更新する。
    *   **ロジックの実行**: `ILevelService`インターフェースを通じて、タイル情報の取得、ブロックの破壊、スポーン地点の計算などの機能を提供する。
    *   **破壊処理APIの分離**: ブロックの破壊は、その意図に応じて2つの明確なAPIに分離されています。
        *   `DestroyConnectedBlocks`: プレイヤーのタイピング成功時に呼び出され、隣接する同種ブロックをまとめて破壊する**連鎖破壊**を実行します。
        *   `DestroyBlockAt`: アイテム効果などから呼び出され、指定された座標のブロックを1つだけ破壊する**単体破壊**を実行します。
        これにより、将来的に新しい破壊パターンのアイテムを追加する際も、既存のロジックに影響を与えることなく安全に拡張できます。

### **3.2. IMapGenerator.cs (Interface / ScriptableObject)**

*   **役割**: **地形（ブロックタイル）の生成アルゴリズム**をカプセル化する。
*   **インターフェース**: `List<TileData> Generate(long seed, Vector2Int worldOffset, Dictionary<TileBase, int> tileIdMap, Dictionary<string, TileBase> tileNameToTileMap)`
*   **責務**: 与えられたシード値と**ワールド座標オフセット**に基づき、特定の領域の地形データを生成する。

### **3.3. ISpawnPointStrategy.cs (Interface / ScriptableObject)**

*   **役割**: 特定のマップ領域内での**スポーン地点を決定するアルゴリズム**をカプセル化する。
*   **インターフェース**: `List<Vector3Int> GetSpawnPoints(int playerCount, List<Vector3Int> areaWalkableTiles, BoundsInt areaBounds, Vector2Int worldOffset)`
*   **責務**: 自身の担当する領域の地形情報に基づき、安全なスポーン地点を計算する。

### **3.4. 多様なマップ生成アルゴリズム (Strategy Pattern)**

地形の生成ロジックは`IMapGenerator`インターフェースを実装した`ScriptableObject`に完全にカプセル化されており、`GameConfig`から差し替えるだけで様々な種類のマップを簡単に試すことができます。

-   **`TerrainPreset`による設定の共通化**:
    -   各ジェネレーターは、使用するタイルとその出現比率（重み）を定義した`TerrainPreset`アセットを参照します。これにより、「洞窟の地層」「森の地層」といったタイルの構成情報を、複数のジェネレーターで再利用できます。

-   **実装済みのジェネレーター**:
    -   **`PerlinNoiseMapGenerator`**: シンプルなパーリンノイズに基づき、滑らかな洞窟を生成します。
    -   **`FbmCaveGenerator`**: 複数のノイズを合成するfBM（フラクショナル・ブラウン運動）アルゴリズムを用い、より自然で複雑なディテールを持つ洞窟を生成します。
    -   **`CellularAutomataGenerator`**: 「生命の誕生と死」のルールをシミュレートし、ゴツゴツとした有機的な洞窟を生成します。
    -   **`VoronoiGenerator`**: マップに配置した「核」からの距離に基づき、細胞がひしめき合ったような独特の地形を生成します。
    -   **`RandomWalkGenerator`**: 「歩行者」がランダムに移動した軌跡を床として削り出すことで、自然な細い通路を生成します。
    -   **`BspDungeonGenerator`**: 空間を再帰的に分割する手法で、四角い部屋と直線的な通路で構成された、構造的なダンジョンを生成します。

## **4. チャンクベース同期システムの動作フロー**

1.  **サーバー起動時**:
    *   `GameManager`が`MapGenerationRequest`を構築し、`LevelManager.GenerateWorld(request)`を呼び出す。
    *   `LevelManager`はリクエストに従い、全ての`SpawnArea`のマップデータを生成し、チャンクごとに分割してメモリ上に保持する。
2.  **プレイヤー参加/移動時**:
    *   `LevelManager`は、プレイヤーの位置に基づき、アクティブにすべきチャンクを計算する。
    *   `NetworkList<TileData>`の内容を更新し、クライアントが必要なデータのみを動的に同期させる。
3.  **クライアント側**:
    *   クライアントの`LevelManager`は、`NetworkList`の変更イベントを購読し、ローカルの`Tilemap`の表示を更新する。

このイベント駆動のチャンクシステムにより、広大で複雑な構成のマップでも、クライアントが必要なデータのみを効率的に同期することが可能になります。

### **全体のドキュメント:**　
[README.md](../../../README.md)
### **関連ドキュメント:**
* [Player-Design.md](../Player/Player-Design.md)  
* [Folder-Structure.md](../../../Folder-Structure.md)