# **UI機能 設計ドキュメント**

## **1\. 設計思想**

UIシステムの設計は\*\*「UIは“愚かな”ビューであるべき」\*\*という原則に基づきます。UIはゲームの状態を表示し、ユーザー入力を通知する以上の責任を持つべきではありません。

* **一方向データフロー**: データの流れは常に「ゲームロジック → UI」の一方向に限定します。UIがゲームの状態を直接書き換えることはありません。  
* **疎結合**: UIコンポーネントは、GameManagerやPlayerといったゲームロジックの具体的なクラスを一切知りません。Readerインターフェースを通じてのみデータを受け取ります。  
* **責務の分割**: UI全体を管理する「画面Manager」と、個々の部品（ゲージ、ラベルなど）を管理する「自己完結型コンポーネント」に役割を分割します。

## **2. 新しいUIアーキテクチャの主要コンポーネント**

リファクタリングにより、UIの責務は以下の汎用コンポーネントと、シーン固有コンポーネントに明確に分割されました。

### **2.1. 汎用UI基盤 (in `Common/` folder)**

#### **`InteractiveButton.cs` (標準ボタン)**
*   **役割**: プロジェクトの標準となる、インタラクション機能付きのボタン。
*   **機能**: `UnityEngine.UI.Button`を継承し、以下の機能を自動で提供します。
    *   マウスホバー時の拡大エフェクトと効果音再生。
    *   クリック時の縮小エフェクトと効果音再生。
*   **利点**: 全てのボタンで一貫したユーザー体験を提供し、個別のエフェクト実装の手間を省きます。

#### **`ScreenBase.cs` (画面の基底クラス)**
*   **役割**: フェードイン/アウト機能を持つ、全てのUIパネル/スクリーンの基底となる抽象クラス。
*   **機能**: `CanvasGroup`を利用して、`Show()`でフェードイン、`Hide()`でフェードアウトする仮想メソッドを提供します。

#### **`UIManager.cs` (画面遷移の実行役)**
*   **役割**: `ScreenBase`を継承した画面間の遷移を、フェード効果付きで実行する汎用マネージャー。
*   **責務**: `ShowScreen(ScreenBase screen)`メソッドを持ち、現在表示中の画面をフェードアウトさせ、次に指定された画面をフェードインさせる単純な責務を持ちます。

### **2.2. シーン固有のUI管理**

#### **`MainMenuManager.cs`, `GameUIManager.cs` (UIの司令塔)**
*   **役割**: 各シーン（MainMenu, Game）におけるUI全体の司令塔。
*   **責務**:
    *   `UIManager`や、自身の管理下にある各種`ScreenBase`コンポーネントへの参照を保持します。
    *   `GameState`の変更や、`AppManager`からのイベントを購読します。
    *   ゲームの状態に応じて、**どの画面を表示すべきかを判断**し、`UIManager.ShowScreen()`を呼び出して実際の画面遷移を**依頼**します。
    *   UI上のボタンイベントを購読し、`AppManager`にゲーム開始を依頼するなど、コアロジックへの橋渡しを行います。

## **3. データ更新のフロー**

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

## **4. 画面レイアウト設計**

### **4.1. 基本原則**

*   **情報の優先度:** 最も重要な情報（酸素、タイピングUI）は、画面中央や視線の動きが少ない場所に配置する。補助的な情報（ミニマップ、スコア）は画面の四隅に配置する。
*   **アンカーとマージン:** 全てのUI要素は、画面の四隅や辺を基準（アンカー）として配置する。これにより、異なる画面アスペクト比でもある程度レイアウトが保たれる。
*   **画面分割への対応:** UI要素は、`Screen Space - Camera`モードを基本とし、各プレイヤー専用のカメラに追従する。これにより、画面が分割されても、各プレイヤーの領域内にUIが正しく表示される。

### **4.2. 主要画面のレイアウト**

**4.2.1. InGameHUD**

ゲームプレイ中のメイン画面です。

*   **Player Status:** 各プレイヤーの酸素ゲージやスコアを表示する領域。画面分割時は、それぞれのカメラ領域の隅に配置されます。
*   **TypingUI:** プレイヤーがタイピング状態に入った際、画面中央下部にモーダル的に表示されるUIです。このUIの表示・非表示およびテキスト更新は、`TypingView.cs`コンポーネントが責務を持ちます。
*   **Minimap / ItemSlots:** 補助的な情報を表示する領域です。
*   **改善**: UIの表示切り替えは、`ScreenBase`と`UIManager`の導入により、`CanvasGroup`のアルファ値を操作する滑らかなフェードイン/アウトに改善されました。

```mermaid
graph TD
    subgraph InGameHUD
        direction TB
        
        subgraph TopArea [ヘッダーエリア]
            Player1Status("自ステータス @左上") --- Player2Status("他ステータス @右上")
        end

        subgraph CenterArea [ゲームプレイエリア]
            TypingUI("タイピングUI @中央下部")
        end

        subgraph BottomArea [フッターエリア]
            Minimap("ミニマップ @左下") --- ItemSlots("アイテム @右下")
        end
    end
```

**4.2.2. ResultScreen**

ゲーム終了後に表示されるリザルト画面です。

```mermaid
graph TD
    ResultTitle("勝敗結果<br>例: 'YOU WIN'")
    PlayerScores("各プレイヤーの最終スコア")
    ActionButtons("再戦 / メインメニューへ")
    
    ResultTitle --> PlayerScores --> ActionButtons
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