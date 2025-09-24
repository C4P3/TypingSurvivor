# **Item機能 設計ドキュメント**

## **1\. 責務**

Item機能は、ゲーム内に登場する全てのアイテム（消耗品、装備品、パッシブ効果を持つものなど）のデータ管理と、その効果の発動ロジックを担います。

**設計目標:**

* **高い拡張性**: プログラマーがコードを修正することなく、企画担当者が新しいアイテムとその効果を簡単に追加・調整できること。  
* **関心の分離**: アイテムの「データ（見た目や設定）」と「振る舞い（効果のロジック）」を完全に分離すること。  
* **疎結合**: アイテムの効果ロジックが、GameManagerやPlayerControllerといった他のシステムの具体的な実装を一切知ることなく動作すること。

## **2\. 主要コンポーネント**

この設計は**ストラテジーパターン**を全面的に採用します。

### **2.1. ItemData.cs (ScriptableObject)**

* **役割**: アイテムの**静的なデータ**を保持するアセット。アイテム1種類につき1つの.assetファイルを作成します。  
* **保持するデータ**:  
  * string ItemName: アイテム名。  
  * Sprite Icon: UIに表示するアイコン。  
  * TileBase ItemTile: マップ上に配置されるタイル。  
  * List\<IItemEffect\> Effects: このアイテムが持つ\*\*効果（のScriptableObjectアセット）\*\*への参照リスト。これにより、複数の効果を組み合わせることができます。

### **2.2. IItemEffect.cs (Interface / ScriptableObject)**

* **役割**: アイテムの\*\*効果（振る舞い）\*\*そのものをカプセル化したアセット。ScriptableObjectとして作成し、具体的なロジックを実装します。  
* **インターフェース**:  
  * void Execute(ItemExecutionContext context);: 効果を実行する唯一のメソッド。

### **2.3. ItemExecutionContext.cs**

* **役割**: IItemEffectが効果を実行するために必要な、\*\*全ての依存関係（外部サービスへの窓口）\*\*をまとめた「道具箱」クラス。  
* **目的**: IItemEffectが他のシステムを直接参照するのを防ぎ、疎結合を維持します。  
* **保持する参照（インターフェース）**:  
  * ulong UserId: 効果を発動したプレイヤーのID。  
  * IGameStateWriter GameStateWriter: 酸素やスコアを操作する。  
  * ILevelService LevelService: ブロックを破壊する。  
  * IPlayerStatusSystemWriter PlayerStatusSystemWriter: プレイヤーの永続ステータスを変更する。

## **3\. サービスとレジストリ**

### **3.1. ItemService.cs**

* **役割**: IItemServiceインターフェースを実装する、サーバーサイドの実行エンジン。  
* **責務**:  
  1. PlayerFacadeからAcquireItem(itemId)の要求を受け取る。  
  2. ItemRegistryに問い合わせ、対応するItemDataを取得する。  
  3. DIで注入された各種Writerサービスへの参照をまとめ、ItemExecutionContextを生成する。  
  4. ItemDataが持つEffectsリストをループし、全てのIItemEffect.Execute(context)を呼び出す。

### **3.2. ItemRegistry.cs (ScriptableObject)**

* **役割**: ゲーム内に存在する全てのItemDataアセットをリストとして保持する、静的な**データベース**。  
* **責務**:  
  * LevelManagerがマップ生成時にランダムなアイテムを選ぶ際や、ItemServiceがIDからItemDataを取得する際に利用される。

## **4\. 設計の利点（ローグライクモードへの拡張性）**

この設計により、将来的な拡張が極めて容易になります。

* **即時効果アイテム**: 「酸素回復」は、IGameStateWriterを利用するOxygenHealEffectを実装します。  
* **永続効果アイテム**: 「移動速度5%UP」は、IPlayerStatusSystemWriterを利用するMoveSpeedUpEffectを実装します。  
* **複合効果アイテム**: 「体力を少し回復し、移動速度も永続的に上げるポーション」は、ItemDataのEffectsリストにOxygenHealEffectとMoveSpeedUpEffectの両方を登録するだけで実現できます。

### **全体のドキュメント:**　
[../../../README.md](../../../README.md)
### **関連ドキュメント:**
* **[./Item-List.md](./Item-List.md)**: 具体的なアイテムの性能やパラメータを定義する**ゲームデザイン仕様書**。
* [../Gameplay/Gameplay-Design.md](../Gameplay/Gameplay-Design.md)  
* [../../../Data-Flow.md](../../../Data-Flow.md)