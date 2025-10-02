# **Matchmaking機能 設計ドキュメント**

## **1. 責務と目的**

このドキュメントは、Unity Gaming Services (UGS) の **Matchmaker** と **Relay** を利用した、オンラインマルチプレイのマッチング機能全体の設計を定義します。

**目的:**

*   **関心の分離**: UGSとの複雑な通信ロジックをUIから完全に分離し、テストと保守が容易な構造を実現する。
*   **堅牢なフロー**: フリーマッチ、ランクマッチ、合言葉マッチ（プライベートマッチ）の全てに対応し、タイムアウト、キャンセル、エラー発生時にも安定したユーザー体験を提供する。
*   **既存アーキテクチャとの調和**: `AppManager` をサービスロケーターとして利用し、`MainMenu`シーン内でUIとロジックが連携する、既存の設計思想を踏襲する。

---

## **2. システム構成図**

マッチング機能は、UI層、コントローラー層、サービス層の3層に明確に分離されます。

```mermaid
graph TD
    subgraph "UI Layer (MainMenu Scene)"
        A[MainMenuManager]
        B[MatchmakingPanel UI]
    end

    subgraph "Controller Layer (MainMenu Scene)"
        C[MatchmakingController]
    end

    subgraph "Service Layer (Core Feature)"
        D[MatchmakingService]
    end

    subgraph "Unity Gaming Services (Cloud)"
        E[UGS Matchmaker]
        F[UGS Relay]
    end

    A -- "1. StartMatchmaking('FreeMatch')" --> C
    C -- "2. Show 'Searching...' UI" --> B
    C -- "3. CreateTicketAsync()" --> D
    D -- "4. UGS API Call" --> E
    E -- "8. Match Found (with Relay Info)" --> D
    D -- "9. OnMatchSuccess Event" --> C
    C -- "10. Update UI to 'Found!'" --> B
    C -- "11. StartClientWithRelay(info)" --> G[AppManager]
    G -- "12. Connect to Relay" --> F
    G -- "13. Load Game Scene" --> H((Game Scene))

```

---

## **3. 主要コンポーネント**

### **3.1. `MatchmakingService.cs` (サービス層)**

*   **配置場所**: `Assets/_Project/Features/Core/Matchmaking/Scripts/`
*   **役割**: UGS MatchmakerおよびRelayとの通信を全て担当する、UIに依存しないプレーンなC#クラス。`AppManager`によってシングルトンとして管理されます。
*   **責務**:
    *   `CreateTicketAsync(playerProfile, queueName)`: マッチングチケットを作成する。
    *   `CheckTicketStatusAsync()`: 定期的にチケットのステータスを確認する。
    *   `CancelMatchmaking()`: 現在のチケットを削除する。
    *   `JoinByCodeAsync(joinCode)`: Relayのルームに合言葉で参加する。
    *   **イベント**:
        *   `OnMatchSuccess(MatchResult result)`: マッチング成功時にRelayの接続情報と共に発行される。
        *   `OnMatchFailure(string reason)`: マッチング失敗時に発行される。
        *   `OnStatusUpdated(string status)`: UI表示用のステータス更新通知。

### **3.2. `MatchmakingController.cs` (コントローラー層)**

*   **配置場所**: `Assets/_Project/Features/UI/Screens/MainMenu/Scripts/`
*   **役割**: `MainMenuManager`からの指示と`MatchmakingService`のイベントを仲介し、UIの状態を制御する。
*   **責務**:
    *   `StartMatchmaking(queueName)`: フリーマッチやランクマッチの開始をトリガーする。
    *   `StartPrivateMatch()`: ホストとしてプライベートマッチ用のRelayルームを作成する。
    *   `JoinPrivateMatch(joinCode)`: 合言葉でプライベートマッチに参加する。
    *   `Cancel()`: 進行中の全てのマッチング処理をキャンセルする。
    *   `MatchmakingService`からのイベントを購読し、`MatchmakingPanel`のテキストを「検索中...」「対戦相手が見つかりました」「ルームを作成中...」のように更新する。
    *   マッチング成功後、`AppManager`にRelay情報を渡してゲームシーンへの遷移を開始させる。

### **3.3. UIコンポーネント (UI層)**

*   **`MultiplayerModeSelectPanel`**: 「フリーマッチ」「ランクマッチ」「合言葉マッチ」のボタンを持つパネル。
*   **`MatchmakingPanel`**: マッチング中のステータスを表示し、「キャンセル」ボタンを持つパネル。
*   **`RoomCodePanel`**: 「ルーム作成」「ルーム参加」の選択肢と、合言葉の入力フィールドを持つパネル。

---

## **4. データフロー**

### **フロー①: フリーマッチ**

```mermaid
sequenceDiagram
    participant User
    participant MainMenuManager
    participant MatchmakingController
    participant MatchmakingService
    participant UGS_Matchmaker
    participant AppManager

    User->>MainMenuManager: 「フリーマッチ」ボタンをクリック
    MainMenuManager->>MatchmakingController: StartMatchmaking("FreeMatch")
    
    MatchmakingController->>MatchmakingService: CreateTicketAsync("FreeMatch")
    MatchmakingController->>MainMenuManager: "マッチング中..."パネル表示を依頼
    
    MatchmakingService->>UGS_Matchmaker: チケット作成リクエスト
    
    loop チケットステータス確認 (数秒ごと)
        MatchmakingService->>UGS_Matchmaker: チケットステータス確認リクエスト
        UGS_Matchmaker-->>MatchmakingService: (ステータス: 検索中)
        MatchmakingService->>MatchmakingController: OnStatusUpdated("検索中...")
    end
    
    UGS_Matchmaker-->>MatchmakingService: (ステータス: 成功, Relay情報)
    MatchmakingService->>MatchmakingController: OnMatchSuccess(RelayInfo)
    
    MatchmakingController->>AppManager: StartClientWithRelay(RelayInfo)
    AppManager->>AppManager: Gameシーンへ遷移
```

### **フロー②: 合言葉マッチ (ホスト)**

```mermaid
sequenceDiagram
    participant User
    participant MainMenuManager
    participant MatchmakingController
    participant MatchmakingService
    participant UGS_Relay
    participant AppManager

    User->>MainMenuManager: 「合言葉マッチ」→「ルーム作成」をクリック
    MainMenuManager->>MatchmakingController: StartPrivateMatch()
    
    MatchmakingController->>MatchmakingService: CreateRelayHostAsync()
    MatchmakingService->>UGS_Relay: ルーム作成リクエスト
    UGS_Relay-->>MatchmakingService: (成功, JoinCode)
    
    MatchmakingService->>MatchmakingController: OnPrivateRoomCreated(JoinCode)
    MatchmakingController->>MainMenuManager: UIにJoinCodeを表示させる
    
    MatchmakingController->>AppManager: StartHostWithRelay(RelayInfo)
    AppManager->>AppManager: Gameシーンへ遷移
```

---

## **5. 実装ステップ**

1.  **UGS SDKの導入**: `com.unity.services.matchmaker` と `com.unity.services.relay` パッケージをプロジェクトに追加する。
2.  **`MatchmakingService`の実装**: UGSのドキュメントを参考に、チケット作成、ステータス確認、Relayのホスト/参加などの非同期メソッドを実装する。
3.  **`MatchmakingController`の実装**: `MatchmakingService`をラップし、UIからの入力を受け付けてサービスを呼び出し、サービスからのイベントに応じてUIを更新するロジックを実装する。
4.  **UIパネルの作成と連携**: `MainMenu`シーンに各UIパネルを作成し、`MainMenuManager`と`MatchmakingController`から制御できるように接続する。
5.  **`AppManager`の拡張**: Relay情報を受け取って`UnityTransport`に設定し、`StartHost()` / `StartClient()` を呼び出すメソッドを追加する。
