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
  * OnPlayerSpawned\_Server(ulong clientId, Vector3 spawnPosition): LevelManagerなどが購読し、プレイヤーの初期状態を設定する。

## **4. ゲームプレイフローとステート移行**

### **4.1. 壁衝突によるタイピングへの移行**

移動先に破壊可能なブロック（壁）が存在した場合、プレイヤーは移動を停止し、そのブロックを破壊するためのタイピングモードに移行します。

*   **サーバーサイドロジック**:
    1.  `PlayerFacade`の`ContinuousMove_Server`コルーチン内で、移動先の`IsWalkable`をチェックします。
    2.  もし`false`だった場合、サーバーは移動を中断します。
    3.  サーバーは、プレイヤーの`_currentState`を`PlayerState.Typing`に変更します。
    4.  タイピング対象のブロックの座標を、新しく追加する`NetworkVariable<Vector3Int> NetworkTypingTargetPosition`に保存し、全クライアントに同期します。
*   **クライアントサイドロジック**:
    1.  `_currentState`の変更を検知し、`PlayerStateMachine`が`TypingState`に遷移します。
    2.  `TypingState`の`Enter`メソッド内で、`PlayerInput`の入力アクションマップを`Typing`に切り替えます。

### **4.2. 入力仕様**

タイピングと移動のアクションが衝突しないよう、また直感的な操作を提供するために、入力仕様を以下のように定めます。

*   **移動**:
    *   `Move`アクション（WASD）は、`MoveInteract`アクション（Shiftなど）が**同時に押されている**場合にのみ有効な移動要求として扱われます。
    *   この判定は、`PlayerInput`クラスがコードで直接`MoveInteract`アクションの状態を読み取ることで実現され、Input Systemの複雑な設定（ModifierやInteraction）を不要にします。

### **4.3. アクションマップの統一**

ユーザー体験の向上と実装の簡素化のため、複数のアクションマップ（`Gameplay`, `Typing`）を切り替える方式を廃止し、単一の`Gameplay`アクションマップに統一します。

*   **設計思想**:
    *   クライアントからの入力（「移動したい」など）は常に同じアクションを通じて発行されます。
    *   サーバーが、プレイヤーの現在の権威あるステート (`_currentState`) に基づいて、その入力の「意図」をインテリジェントに解釈し、振る舞いを決定します。
*   **`Move`アクションの解釈**:
    *   プレイヤーが`Roaming`状態の時に`Move`入力があれば、サーバーはそれを「**移動開始**」と解釈します。
    *   プレイヤーが`Moving`状態の時に`Move`入力があれば、サーバーはそれを「**方向転換**」と解釈します。
    *   プレイヤーが`Typing`状態の時に`Move`入力があれば、サーバーはそれを「**タイピングを中断して、即座にその方向へ移動を開始する**」という複合的な意図として解釈します。
*   **その他のアクション**:
    *   `CancelTyping`（Enterキー）や`PauseGame`（Escキー）といった、特定の状況でのみ意味を持つアクションも`Gameplay`マップに追加されます。これらのアクションが実行されるかどうかは、UI ManagerやTyping Managerといった、それぞれの文脈を管理するシステムが判断します。

\
\
### **全体のドキュメント:**　
[../../../README.md](../../../README.md)
### **関連ドキュメント:**
* [./Level/Level-Design.md](../Level/Level-Design.md)  
* [../Data-Flow.md](../../../Data-Flow.md)