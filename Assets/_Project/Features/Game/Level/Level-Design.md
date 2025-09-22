# **Level機能 設計ドキュメント**

## **1\. 責務**

Level機能は、ゲームの舞台となる**世界の物理的な状態**を管理します。プレイヤーやアイテムが存在する「空間」そのものに対する全ての責務を持ちます。

* ゲーム開始時に、アルゴリズムに基づいてマップ（ブロック、アイテムの初期配置）を生成する。  
* 広大なマップを効率的に扱うため、プレイヤーの周囲の**チャンク**のみをクライアントと同期する。  
* プレイヤーの移動に応じて、動的にチャンクをロード/アンロードする。  
* 外部からの要求（DestroyBlockなど）に応じてマップの状態を変更し、その結果を全クライアントに同期させる。

## **2\. 主要コンポーネント**

### **2.1. LevelManager.cs**

* **役割**: ILevelServiceインターフェースを実装する、サーバーサイドのメインクラス。NetworkBehaviourを継承します。  
* **責務**:  
  * **状態の所有**: NetworkList\<TileData\>として、現在アクティブな（同期対象の）タイルデータを保持する。  
  * **チャンク管理**: 全プレイヤーの位置を監視し、どのチャンクをアクティブにすべきかを判断し、NetworkListの内容を更新（ロード/アンロード）する。  
  * **ロジックの実行**: DestroyBlockなどの命令を受け、NetworkListやサーバーが保持する全マップデータを変更する。  
  * **イベントの発行**: ブロックが破壊された際などに、OnBlockDestroyed\_Serverのようなサーバーサイドイベントを発行し、GameManagerなどの他システムに事実を通知する。

### **2.2. IMapGenerator.cs (Interface / ScriptableObject)**

* **役割**: **マップ生成アルゴリズム**をカプセル化する。**ストラテジーパターン**を採用。  
* **インターフェース**: (List\<TileData\> blockTiles, List\<TileData\> itemTiles) Generate(long seed);  
* **実装例**: PerlinNoiseMapGenerator.csなど。これにより、マップ生成ロジックをLevelManagerから完全に分離し、アルゴリズムの差し替えを容易にします。

### **2.3. TileData.cs (struct)**

* **役割**: タイルの情報をネットワークで同期するための、カスタムデータ構造体。  
* **実装要件**:  
  * **INetworkSerializable**: データをネットワーク送信用にシリアライズ/デシリアライズする方法を定義します。  
  * **IEquatable\<TileData\>**: NetworkListが内部で要素を比較（検索、削除など）するために必要な、等価判定のルールを定義します。  
* **保持するデータ**:  
  * Vector3Int Position: タイルのグリッド座標。  
  * int TileId: TileBaseアセットを識別するための整数ID。

## **3\. チャンクベース同期システムの動作フロー**

1. **サーバー起動時**:  
   * LevelManagerはIMapGeneratorを使い、全マップデータを生成し、チャンクごとに分割してメモリ上に保持します（Dictionary\<Vector2Int, List\<TileData\>\>）。  
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