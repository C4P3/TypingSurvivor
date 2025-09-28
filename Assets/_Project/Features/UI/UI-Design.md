# **UI機能 設計ドキュメント**

## **1\. 設計思想**

UIシステムの設計は\*\*「UIは“愚かな”ビューであるべき」\*\*という原則に基づきます。UIはゲームの状態を表示し、ユーザー入力を通知する以上の責任を持つべきではありません。

* **一方向データフロー**: データの流れは常に「ゲームロジック → UI」の一方向に限定します。UIがゲームの状態を直接書き換えることはありません。  
* **疎結合**: UIコンポーネントは、GameManagerやPlayerといったゲームロジックの具体的なクラスを一切知りません。Readerインターフェースを通じてのみデータを受け取ります。  
* **責務の分割**: UI全体を管理する「画面Manager」と、個々の部品（ゲージ、ラベルなど）を管理する「自己完結型コンポーネント」に役割を分割します。

## **2\. 主要コンポーネント**

### **2.1. GameUIManager.cs (UIの司令塔)**

* **役割**: `Game`シーン全体のUIの状態を管理する唯一の司令塔。
* **責務**:
  * `InGameHUD`, `ResultScreen`, `CountdownUI`など、シーンに存在するUIスクリーンへの参照をインスペクターから保持します。
  * `IGameStateReader`の`CurrentPhaseNV`イベントを購読します。
  * `GamePhase`の変更を検知し、現在のフェーズに合ったUIスクリーンのみを表示し、他を非表示にする責務を持ちます。
  * `ResultScreen`のボタンクリックのようなUIからのイベントを購読し、`GameManager`へのRPC呼び出しなど、適切なゲームロジックへの通知に変換します。

### **2.2. UIスクリーン / コンポーネント (例: ResultScreen.cs, OxygenView.cs)**

* **役割**: `ResultScreen`のような画面単位のUIや、`OxygenView`のような単一責務のUI部品。これらはもはや`Manager`である必要はありません。
* **責務**:
  * 自身の見た目（Slider, Buttonなど）への参照を保持します。
  * 外部（`GameUIManager`）から呼び出されるための公開メソッド（例: `Show(string message)`, `UpdateView(float value)`)と、外部が購読するためのC#イベント（例: `OnRematchClicked`）を持ちます。
  * 渡されたデータに基づいて自身の見た目を更新したり、ユーザーの入力をイベントとして通知することにのみ責任を持ちます。

## **3\. データ更新のフロー**

ゲームの状態が変更され、UIの表示が更新されるまでの流れは以下のようになります。

```mermaid
sequenceDiagram  
    participant S_Logic as ゲームロジック (Server)  
    participant NV as NetworkVariable (Sync)  
    participant C_Reader as Reader I/F (Client)  
    participant C_Manager as 画面Manager (Client)  
    participant C_View as UIコンポーネント (Client)

    S_Logic->>S_Logic: 状態を変更  
    note right of S_Logic: GameManagerが<br>OxygenLevel.Valueを更新  
      
    NV-->>C_Reader: OnValueChanged イベント発火  
    note left of C_Reader: クライアントのGameManagerが検知  
      
    C_Reader->>C_Manager: OnOxygenChanged イベント発行  
    note left of C_Manager: InGameHUDManagerがイベントを購読  
      
    C_Manager->>C_View: UpdateView(newValue)  
    note right of C_View: OxygenViewが表示を更新
```
この一方向のデータフローにより、UIのロジックは非常にシンプルかつ予測可能になり、デバッグや仕様変更が容易になります。

## **4\. 画面レイアウト設計**

各画面の主要な構成要素と、その配置や比率に関する基本的な考え方を記述します。

### **4.1. InGameHUD**

ゲームプレイ中のメイン画面です。プレイヤーに必要な情報を、視線を大きく動かすことなく確認できるように配置します。

```mermaid
graph TD
    subgraph InGameHUD
        direction TB
        
        subgraph TopArea [ヘッダーエリア (画面上部)]
            Player1Status(自プレイヤーのステータス<br>左上) --- Player2Status(他プレイヤーのステータス<br>右上)
        end

        subgraph CenterArea [ゲームプレイエリア (中央)]
            TypingUI(タイピングUI<br>中央下部に表示)
        end

        subgraph BottomArea [フッターエリア (画面下部)]
            Minimap(ミニマップ<br>左下) --- ItemSlots(所持アイテム<br>右下)
        end
    end
```

*   **Player Status:** 各プレイヤーの酸素ゲージやスコアを表示する領域。画面分割時は、それぞれのカメラ領域の隅に配置されます。
*   **TypingUI:** タイピング対象のブロックが出現した際に、画面中央下部にモーダル的に表示されるUIです。
*   **Minimap / ItemSlots:** 補助的な情報を表示する領域です。

### **4.2. ResultScreen**

ゲーム終了後に表示されるリザルト画面です。

```mermaid
graph TD
    subgraph ResultScreen
        direction TB
        
        ResultTitle(勝敗結果<br>例: "YOU WIN")
        PlayerScores(各プレイヤーの最終スコア)
        ActionButtons(再戦 / メインメニューへ)
        
        ResultTitle --> PlayerScores --> ActionButtons
    end
```

## **5\. 画面遷移図**

アプリケーション全体の画面フローと状態遷移を示します。

```mermaid
stateDiagram-v2
    direction LR
    [*] --> MainMenu: アプリ起動

    MainMenu --> Matching: マルチプレイ開始
    MainMenu --> InGameHUD: シングルプレイ開始
    
    Matching --> InGameHUD: マッチング成功
    Matching --> MainMenu: キャンセル

    InGameHUD --> ResultsScreen: ゲーム終了
    InGameHUD --> DisconnectPopup: 接続断
    
    ResultsScreen --> InGameHUD: 再戦
    ResultsScreen --> MainMenu: メインメニューへ
    ResultsScreen --> DisconnectPopup: 接続断

    DisconnectPopup --> MainMenu: 確認ボタン押下
```

**関連ドキュメント:**

* [../Game/Gameplay/Gameplay-Design.md](../Game/Gameplay/Gameplay-Design.md)  
* [../Architecture-Overview.md](../../Architecture-Overview.md)