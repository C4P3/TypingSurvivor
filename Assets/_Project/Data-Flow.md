# **データフローの例**

このドキュメントでは、本アーキテクチャにおける代表的な処理が、どのようにシステム間を連携して実行されるかをシーケンス図を用いて解説します。

これにより、各機能がインターフェースを通じてどのように疎結合に連携しているかを具体的に理解することができます。

## **1\. フロー①：プレイヤーが移動し、チャンクが更新される**

プレイヤーの入力が、サーバーサイドのチャンク更新ロジックに繋がり、最終的にクライアントの表示に反映されるまでの流れです。

```mermaid
sequenceDiagram  
    participant C_Input as PlayerInput (Client)  
    participant C_Facade as PlayerFacade (Client)  
    participant S_Facade as PlayerFacade (Server)  
    participant S_Level as ILevelService (Server)
    participant C_Level as LevelManager (Client)

    C_Input->>C_Facade: OnMoveIntent (移動の意図を通知)  
    C_Facade->>S_Facade: CmdRequestMove(direction)  
    note right of S_Facade: サーバーが移動処理を実行し、<br>プレイヤーの位置(Transform)を更新  
      
    S_Facade->>S_Facade: OnPlayerMoved_Server イベント発行  
    note left of S_Level: LevelManagerがイベントを購読している  
    S_Level->>S_Level: HandlePlayerMoved(clientId, newPosition)  
    note right of S_Level: チャンク更新が必要か判断し、<br>NetworkListを更新 (ロード/アンロード)

    S_Level-->>C_Level: NetworkList<TileData> の変更が自動同期  
    note left of C_Level: クライアントのLevelManagerが<br>OnListChangedイベントで変更を検知し、<br>ローカルのTilemapの表示を更新する
```
## **2\. フロー②：タイピングでブロックを破壊し、スコアが加算され、UIが更新される**

クライアントのタイピング成功が、サーバーでのブロック破壊とスコア加算に繋がり、その結果がクライアントのUIに反映されるまでの一連の流れです。
```mermaid
sequenceDiagram  
    participant C_Typing as TypingManager (Client)  
    participant C_Facade as PlayerFacade (Client)  
    participant S_Facade as PlayerFacade (Server)  
    participant S_Level as ILevelService (Server)  
    participant S_Game as IGameStateWriter (Server)  
    participant C_Game as IGameStateReader (Client)  
    participant C_UI as InGameHUDManager (Client)

    C_Typing->>C_Facade: OnTypingSuccess (イベント通知)  
    C_Facade->>S_Facade: CmdDestroyBlock(blockPos)  
      
    S_Facade->>S_Level: DestroyBlock(clientId, blockPos)  
    note right of S_Level: ブロックを破壊し、<br>OnBlockDestroyed_Server イベントを発行

    note left of S_Game: GameManagerがイベントを購読  
    S_Game->>S_Game: AddScore(clientId, 10)  
    note right of S_Game: ScoreのNetworkVariableを更新  
      
    S_Game-->>C_Game: NetworkVariable<int> の変更が自動同期  
    note left of C_Game: クライアントのGameManagerが<br>OnValueChangedで変更を検知し、<br>OnScoreChangedイベントを発行

    note left of C_UI: HUD Managerがイベントを購読  
    C_UI->>C_UI: HandleScoreChanged(newScore)  
    note right of C_UI: ScoreViewコンポーネントに<br>表示更新を指示
```
**全体のドキュメント:**　[README.md](./README.md)
**次のドキュメント:** [Gameplay-Design.md](./Features/Game/Gameplay/Gameplay-Design.md) (各機能詳細設計へ)