# **Level機能 設計ドキュメント**

## **1\. 責務**

Level機能は、ゲームの舞台となる**世界の物理的な状態**を管理します。プレイヤーやアイテムが存在する「空間」そのものに対する全ての責務を持ちます。

* ゲーム開始時に、**地形生成**と**アイテム配置**のアルゴリズムを組み合わせてマップを生成する。  
* 広大なマップを効率的に扱うため、プレイヤーの周囲の**チャンク**のみをクライアントと同期する。  
* プレイヤーの移動に応じて、動的にチャンクをロード/アンロードする。  
* 外部からの要求（DestroyBlockなど）に応じてマップの状態を変更し、その結果を全クライアントに同期させる。

## **2\. 主要コンポーネント**

### **2.1. LevelManager.cs**

* **役割**: ILevelServiceインターフェースを実装する、サーバーサイドのメインクラス。NetworkBehaviourを継承します。マップ生成とアイテム配置のオーケストレーター（指揮者）としても機能します。
* **責務**:  
  * **マップ生成の指揮**: `IMapGenerator`と`IItemPlacementStrategy`、`ItemRegistry`への参照を保持し、サーバー起動時にこれらを連携させて完全なマップデータを構築する。
  * **状態の所有**: NetworkList<TileData>として、現在アクティブな（同期対象の）タイルデータを保持する。  
  * **チャンク管理**: 全プレイヤーの位置を監視し、どのチャンクをアクティブにすべきかを判断し、NetworkListの内容を更新（ロード/アンロード）する。  
  * **ロジックの実行**: 外部からの要求に応じてマップの状態を変更したり、情報を返したりする。`ILevelService`インターフェースを通じて以下の機能を提供する。
    * `TileBase GetTile(Vector3Int gridPosition)`: 指定座標にあるタイル（ブロックまたはアイテム）を返す。
    * `bool IsWalkable(Vector3Int gridPosition)`: 指定座標が歩行可能かを返す。
    * `bool HasItemTile(Vector3Int gridPosition)`: 指定座標にアイテムタイルが存在するかを返す。
    * `void RemoveItem(Vector3Int gridPosition)`: 指定座標のアイテムをマップから削除する。
    * `void DestroyBlock(ulong clientId, Vector3Int gridPosition)`: 指定座標のブロックを破壊する。
    * **スポーン地点の計算と準備**:
        * `List<Vector3Int> GetSpawnPoints(int playerCount, ScriptableObject strategy)`: `ISpawnPointStrategy` を受け取り、ゲームモードに応じたプレイヤーの初期スポーン地点リストを計算して返す。
        * `void ClearArea(Vector3Int gridPosition, int radius)`: 指定された座標の周辺を指定された半径分、更地にする。
  * **イベントの発行**: ブロックが破壊された際などに、OnBlockDestroyed_Serverのようなサーバーサイドイベントを発行し、GameManagerなどの他システムに事実を通知する。

### **2.2. IMapGenerator.cs (Interface / ScriptableObject)**

* **役割**: **地形（ブロックタイル）の生成アルゴリズム**をカapsule化する。**ストラテジーパターン**を採用。  
* **インターフェース**: `List<TileData> Generate(long seed, Dictionary<TileBase, int> tileIdMap);`  
* **責務**: 地形の生成にのみ責任を持ち、アイテムの配置については関知しない。
* **実装例**: PerlinNoiseMapGenerator.csなど。

### **2.3. IItemPlacementStrategy.cs (Interface / ScriptableObject)**

* **役割**: **アイテムの配置アルゴリズム**をカプセル化する。**ストラテジーパターン**を採用。
* **インターフェース**: `List<TileData> PlaceItems(List<TileData> blockTiles, ItemRegistry itemRegistry, System.Random prng, Dictionary<TileBase, int> tileIdMap);`
* **責務**: 生成済みの地形情報と登録済みのアイテムリストを受け取り、どこにどのアイテムを配置するかのロジックに責任を持つ。
* **実装例**: `RandomItemPlacementStrategy.cs`（地形の空きスペースに、`ItemRegistry`の重みに基づいてランダムにアイテムを配置する）。

### **2.4. TileData.cs (struct)**

* **役割**: タイルの情報をネットワークで同期するための、カスタムデータ構造体。  
* **実装要件**:  
  * **INetworkSerializable**: データをネットワーク送信用にシリアライズ/デシリアライズする方法を定義します。  
  * **IEquatable\<TileData\>**: NetworkListが内部で要素を比較（検索、削除など）するために必要な、等価判定のルールを定義します。  
* **保持するデータ**:  
  * Vector3Int Position: タイルのグリッド座標。  
  * int TileId: TileBaseアセットを識別するための整数ID。

## **3\. チャンクベース同期システムの動作フロー**

1. **サーバー起動時**:  
   * `LevelManager`はまず`IMapGenerator`を使って**地形（ブロック）**の全データを生成する。
   * 次に、生成された地形データと`ItemRegistry`を`IItemPlacementStrategy`に渡し、**配置するアイテム**の全データを生成する。
   * `LevelManager`は、これらのブロックとアイテムのデータをチャンクごとに分割してメモリ上に保持する。
2. **プレイヤー参加/移動時**:  
   * LevelManagerは、プレイヤーからのスポーン/移動イベント（PlayerFacadeが発行）を受け取ります。  
   * 全プレイヤーの位置を基に、「現在アクティブにすべきチャンク」のセットを計算します。  
   * 現在同期されているチャンクのセットと比較し、差分を求めます。  
     * **ロード**: 新たに必要になったチャンクのタイルデータを、サーバーの全マップデータからNetworkList\<TileData\>にAddします。  
     * **アンロード**: 不要になったチャンクのタイルデータを、NetworkList\<TileData\>からRemoveします。  
3. **クライアント側**:  
   * クライアントのLevelManagerは、NetworkListのOnListChangedイベントを購読します。  
   * Addイベントが来たら、対応するタイルをローカルのTilemapに描画（SetTile）します。  
   * Removeイベントが来たら、対応するタイルをローカルのTilemapから削除します。

このイベント駆動のチャンクシステムにより、広大なマップでもクライアントが必要なデータのみを効率的に同期することが可能になります。

### **全体のドキュメント:**　
[README.md](../../../README.md)
### **関連ドキュメント:**
* [Player-Design.md](../Player/Player-Design.md)  
* [Folder-Structure.md](../../../Folder-Structure.md)