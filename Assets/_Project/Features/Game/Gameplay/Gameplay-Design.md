# **Gameplay機能 設計ドキュメント**

## **1\. 責務**

Gameplay機能は、ゲームの**ルール**そのものを司ります。具体的には以下の責務を持ちます。

* ゲーム全体の進行状態（準備中、プレイ中、終了後など）の管理。  
* 酸素レベルやスコア、制限時間といった、ゲームの勝敗に関わるグローバルな状態の管理と同期。  
* ゲームルールの具体的な実装（シングルプレイとマルチプレイでのルールの違いなど）。  
* 他のシステム（Level, Playerなど）から発行されたサーバーサイドイベントを購読し、ルールに基づいてGameStateを更新すること。

## **2\. 主要コンポーネント**

### **2.1. GameManager.cs**

ゲームプレイシーンにおける最高位の管理クラスであり、サーバーサイドのロジックの起点となります。

* **役割**:  
  * IGameStateReaderおよびIGameStateWriterインターフェースを実装します。  
  * ゲームの「状態」そのものであるNetworkVariable群をフィールドとして所有します。  
  * サーバー上で、時間経過による酸素減少や、他システムからのイベントに応じて状態を更新します。  
  * DI（手動またはコンテナ）の起点となり、各システムに必要なインターフェースを注入する役割も担います。  
* **所有するGameStateデータ**:  
  * NetworkVariable\<float\> OxygenLevel: 現在の酸素レベル。  
  * NetworkVariable\<float\> GameTimer: ゲームの経過時間。  
  * NetworkList\<PlayerData\> PlayerDatas: 各プレイヤーのスコアや状態を保持するリスト。

### **2.2. PlayerData.cs (struct)**

各プレイヤーのルールに関する状態をまとめた、同期用のデータ構造です。

* **INetworkSerializable** と **IEquatable\<PlayerData\>** を実装する必要があります。  
* **保持するデータ**:  
  * ulong ClientId: プレイヤーを識別するID。  
  * int Score: 現在のスコア。  
  * bool IsGameOver: このプレイヤーがゲームオーバーになったかどうかのフラグ。  
  * （必要に応じて）string PlayerNameなど。

### **2.3. GameModeStrategy パターン**

シングルプレイとマルチプレイで、ゲームオーバーの条件やスコア計算のルールが異なる場合に対応するための設計です。

* **IGameModeStrategy (Interface)**:  
  * CheckGameOver(IGameStateReader state): ゲームオーバー条件を判定するメソッドを定義します。  
* **SinglePlayMode.cs / MultiPlayMode.cs**:  
  * 上記インターフェースを実装した具体的なルールクラス。  
  * GameManagerは、ゲーム開始時に適切なStrategyをDIで受け取り、ルールの判定をそのStrategyに委譲します。

## **3\. インターフェース定義**

### **3.1. IGameStateReader.cs**

ゲーム状態の**読み取り**専用インターフェース。UIなど、状態を表示するだけの安全なクラスに渡されます。

* float CurrentOxygen { get; }  
* int GetPlayerScore(ulong clientId) { get; }  
* event Action\<float\> OnOxygenChanged;  
* event Action\<ulong, int\> OnScoreChanged;

### **3.2. IGameStateWriter.cs**

ゲーム状態の**書き込み**を許可するインターフェース。ItemEffectなど、状態を変更する権限を持つ、限定されたクラスにのみ渡されます。

* void AddOxygen(float amount);  
* void AddScore(ulong clientId, int amount);  
* void SetPlayerGameOver(ulong clientId);

### **全体のドキュメント:**　
[./README.md](../../../README.md)
### **関連ドキュメント:**
* [../Player/Player-Design.md](../Player/Player-Design.md)  
* [../UI/UI-Design.md](../UI/UI-Design.md)