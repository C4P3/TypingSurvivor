# **UI機能 設計ドキュメント**

## **1\. 設計思想**

UIシステムの設計は\*\*「UIは“愚かな”ビューであるべき」\*\*という原則に基づきます。UIはゲームの状態を表示し、ユーザー入力を通知する以上の責任を持つべきではありません。

* **一方向データフロー**: データの流れは常に「ゲームロジック → UI」の一方向に限定します。UIがゲームの状態を直接書き換えることはありません。  
* **疎結合**: UIコンポーネントは、GameManagerやPlayerといったゲームロジックの具体的なクラスを一切知りません。Readerインターフェースを通じてのみデータを受け取ります。  
* **責務の分割**: UI全体を管理する「画面Manager」と、個々の部品（ゲージ、ラベルなど）を管理する「自己完結型コンポーネント」に役割を分割します。

## **2\. 主要コンポーネント**

### **2.1. 画面Manager (例: InGameHUDManager.cs)**

* **役割**: UIの「画面」単位（インゲームHUD、メインメニュー、リザルト画面など）の司令塔。  
* **責務**:  
  * DI（手動またはコンテナ）を通じて、必要な\*\*Readerインターフェース\*\*（IGameStateReader, IPlayerStatusSystemReaderなど）への参照を受け取ります。  
  * Readerインターフェースが公開する**イベント**（OnOxygenChangedなど）を購読します。  
  * イベントを受け取ったら、自身の配下にある具体的なUIコンポーネント（OxygenViewなど）を呼び出し、表示の更新を指示します。  
  * 画面内のボタンなどがクリックされた際にイベントを受け取り、AppManager（シーン遷移など）やPlayerFacade（入力の通知）に処理を依頼します。

### **2.2. 自己完結型コンポーネント (例: OxygenView.cs)**

* **役割**: 酸素ゲージやスコアラベルといった、単一の責務を持つ再利用可能なUI部品。  
* **責務**:  
  * 自身の見た目（Slider, Image, TextMeshProUGUIなど）への参照を保持します。  
  * 外部（画面Manager）から呼び出されるための、単一の公開メソッド（例: UpdateView(float currentValue, float maxValue)）を持ちます。  
  * 渡されたデータに基づいて、自身の見た目を更新することにのみ責任を持ちます。ゲームのルールや他のUI部品のことは一切関知しません。

## **3\. データ更新のフロー**

ゲームの状態が変更され、UIの表示が更新されるまでの流れは以下のようになります。

sequenceDiagram  
    participant S\_Logic as ゲームロジック (Server)  
    participant NV as NetworkVariable (Sync)  
    participant C\_Reader as Reader I/F (Client)  
    participant C\_Manager as 画面Manager (Client)  
    participant C\_View as UIコンポーネント (Client)

    S\_Logic-\>\>S\_Logic: 状態を変更  
    note right of S\_Logic: GameManagerが\<br\>OxygenLevel.Valueを更新  
      
    NV--\>\>C\_Reader: OnValueChanged イベント発火  
    note left of C\_Reader: クライアントのGameManagerが検知  
      
    C\_Reader-\>\>C\_Manager: OnOxygenChanged イベント発行  
    note left of C\_Manager: InGameHUDManagerがイベントを購読  
      
    C\_Manager-\>\>C\_View: UpdateView(newValue)  
    note right of C\_View: OxygenViewが表示を更新

この一方向のデータフローにより、UIのロジックは非常にシンプルかつ予測可能になり、デバッグや仕様変更が容易になります。

**関連ドキュメント:**

* [../Game/Gameplay/Gameplay-Design.md](../Game/Gameplay/Gameplay-Design.md)  
* [../Architecture-Overview.md](../../Architecture-Overview.md)