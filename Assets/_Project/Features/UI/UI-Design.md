# **UI機能 設計ドキュメント**

## **1. 設計思想**

UIシステムの設計は、責務の分離とデータ駆動を重視します。UIはゲームの状態を反映する「ビュー」に徹し、ロジックは持ちません。また、UIの見た目や振る舞いの大部分は、再利用可能な基盤コンポーネントによって標準化されます。

---

## **2. UIアーキテクチャ**

### **2.1. 画面の種別定義**

*   **スクリーン (Screen)**: アプリケーションの主要な**全画面表示**を指します。例：「タイトル」「メインメニュー」「ランキング」。一度に表示されるスクリーンは常に一つです。
*   **パネル (Panel)**: スクリーンや他のパネルの上に、一時的に**重ねて表示される**UI部品を指します。例：「難易度選択」「マッチング中」。これらは複数重ねることができます。

### **2.2. ナビゲーション管理 (`UIManager`)**

画面遷移は汎用の`UIManager`が一元管理します。`UIManager`は、ベースとなる「スクリーン」と、その上に積まれる「パネルのスタック」を別々に管理します。

*   **`ShowScreen(screen)`**: 現在表示中の全てのパネルを閉じ、ベーススクリーンを切り替えます。これにより、どのパネルからでも他のスクリーンへクリーンに遷移できます。
*   **`PushPanel(panel)`**: 新しいパネルをスタックに積み、一番手前に表示します。
*   **`PopPanel()`**: スタックの一番上のパネルを取り除き、非表示にします。各パネルの「戻る」ボタンの基本的な動作となります。

### **2.3. シーン固有のUI管理 (`UIFlowCoordinator`, `GameUIManager`)**

各シーンに配置される`UIFlowCoordinator`や`GameUIManager`は、そのシーンにおけるUIの司令塔です。ゲームの状態を監視し、「どのスクリーン・パネルをいつ表示するか」を判断して、`UIManager`に具体的な遷移を依頼する責務を持ちます。

---

## **3. 具体的な画面フロー設計**

### **3.1. タイトルスクリーン (TitleScreen)**
*   **役割**: ゲーム起動後、最初に表示される画面。
*   **構成**: タイトルロゴ、開始ボタン、アカウント連携ボタン。
*   **遷移**: 開始ボタンクリックで`UIManager.ShowScreen(MainMenuScreen)`を呼び出す。

### **3.2. メインメニュー (MainMenuScreen)**
*   **役割**: 全てのゲームモードへの起点となるハブ画面。
*   **構成**: `シングルプレイ`, `マルチプレイ`, `ランキング`, `設定`, `ショップ`, `クレジット` の6ボタン。
*   **遷移**:
    *   `シングルプレイ` → `UIManager.PushPanel(DifficultySelectPanel)`
    *   `マルチプレイ` → `UIManager.ShowScreen(MultiplayerModeSelectScreen)`
    *   `ランキング` → `UIManager.ShowScreen(RankingScreen)`
    *   (設定、ショップ、クレジットも同様)

### **3.3. シングルプレイ選択スクリーン (SinglePlayerSelectScreen)**
*   **役割**: シングルプレイの難易度を選択する。
*   **構成**: 難易度ボタン(3つ)、`戻る`ボタン、`ランキングを見る`ボタン。
*   **遷移**:
    *   `難易度ボタン` → ゲーム開始処理を呼び出す。
    *   `戻る`ボタン → `UIFlowCoordinator.CloseCurrentPanel()`
    *   `ランキングを見る`ボタン → `UIFlowCoordinator.RequestStateChange(PlayerUIState.InRanking)`

### **3.4. マルチプレイモード選択スクリーン (MultiplayerSelectScreen)**
*   **役割**: マルチプレイの対戦形式を選択する。
*   **構成**: `フリーマッチ`, `ランクマッチ`, `合言葉マッチ`ボタン、`戻る`ボタン、`ランキングを見る`ボタン。
*   **遷移**:
    *   `マッチボタン` → `UIFlowCoordinator.StartPublicMatchmaking(...)` or `UIFlowCoordinator.RequestStateChange(PlayerUIState.EnteringMatchCode)`
    *   `戻る`ボタン → `UIFlowCoordinator.CloseCurrentPanel()`
    *   `ランキングを見る`ボタン → `UIFlowCoordinator.RequestStateChange(PlayerUIState.InRanking)`

---

## **4. データ更新フロー**
(変更なし)

## **5. 画面レイアウト設計**
(変更なし)

## **6. 画面遷移図**
(変更なし)