# **Player機能 設計ドキュメント**

## **1\. 責務**

Player機能は、ユーザーが操作するキャラクターに関する全ての責務を管理します。これは、単一の巨大なPlayerControllerクラスではなく、関心事に基づいて複数の専門クラスに分割されます。

* ユーザーからの入力を受け付ける。  
* キャラクターの状態（待機、移動、タイピングなど）を管理し、状態に応じた振る舞いを実行する。  
* サーバーとクライアント間で、位置や状態などの重要なデータを同期する。  
* キャラクターの見た目（アニメーションやエフェクト）を制御する。  
* ローグライクモードなどで得られる永続的なステータス強化を管理する。

## **2\. 主要コンポーネント (Player.prefabにアタッチされる)**

### **2.1. PlayerFacade.cs**

* **役割**: Player機能全体の**司令塔**であり、外部との唯一の**窓口（Facade）**。NetworkBehaviourを継承し、ネットワーク通信の起点となります。  
* **責務**:  
  * PlayerInputからのイベントを購読し、\[ServerRpc\]に変換してサーバーへ処理を要求する。  
  * サーバー上でIItemServiceやILevelServiceなどの外部サービスを呼び出す。  
  * サーバーから同期された状態（NetworkVariable）の変更を検知し、PlayerStateMachineやPlayerViewに更新を指示する。  
  * サーバーサイドでイベント（OnPlayerMoved\_Serverなど）を発行し、LevelManagerなどの他システムに自身の状態変化を通知する。

### **2.2. PlayerInput.cs**

* **役割**: 入力の受付係。MonoBehaviourであり、ネットワークのことは一切関知しません。  
* **責務**:  
  * UnityのInput System（新）からの入力を受け取り、「移動したい」「インタラクトしたい」といった意味のあるC\#イベント（OnMoveIntentなど）に変換して発行する。  
  * このコンポーネントはPlayerFacadeによって、IsOwnerがtrueのクライアントでのみ有効化（enabled \= true）されます。

### **2.3. PlayerStateMachine.cs**

* **役割**: 状態ごとの振る舞いを司る専門家。**ステートパターン**で実装されます。  
* **責務**:  
  * IPlayerStateインターフェース（Enter, Execute, Exitを持つ）と、その具体的な実装クラス（RoamingState, MovingState, TypingState）を管理する。  
  * PlayerFacadeからの指示に基づき、状態を遷移させる。

### **2.4. PlayerView.cs**

* **役割**: 見た目の演出家。  
* **責務**:  
  * アニメーション、パーティクルエフェクト、サウンドエフェクトなど、プレイヤーの見た目や音に関する全ての表現を管理する。  
  * PlayerFacadeから状態変更の通知を受け取り、適切なアニメーションやエフェクトを再生する。

## **3\. 関連システムとインターフェース**

### **3.1. PlayerStatusSystem**

* **役割**: ローグライクモードなどで得られる、永続的なステータス強化（移動速度UP、最大HP上昇など）を管理するサーバーサイドのシステム。  
* **インターフェース**:  
  * **IPlayerStatusSystemReader.cs**: float GetStatValue(ulong clientId, PlayerStat stat)など、現在のステータス値を取得する。主にPlayerFacadeやUIが利用する。  
  * **IPlayerStatusSystemWriter.cs**: void AddPermanentModifier(ulong clientId, PlayerStat stat, float value)など、ステータスを変更する。主にItemEffectから利用される。

### **3.2. サーバーサイドイベント**

* PlayerFacadeは、自身の重要な状態変化をサーバーサイドでイベントとして発行します。  
  * OnPlayerMoved\_Server(ulong clientId, Vector3 newPosition): LevelManagerが購読し、チャンク更新のトリガーとする。  
  * OnPlayerSpawned\_Server(ulong clientId, Vector3 spawnPosition): LevelManagerなどが購読し、プレイヤーの初期状態を設定する。\

### **全体のドキュメント:**　
[../../../README.md](../../../README.md)
### **関連ドキュメント:**
* [./Level/Level-Design.md](../Level/Level-Design.md)  
* [../Data-Flow.md](../../../Data-Flow.md)