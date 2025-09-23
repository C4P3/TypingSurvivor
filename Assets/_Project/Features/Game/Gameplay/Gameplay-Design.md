# **Gameplay機能 設計ドキュメント**

## **1\. 責務**

Gameplay機能は、ゲームの**ルール**そのものを司ります。具体的には以下の責務を持ちます。

* ゲーム全体の進行状態（待機中、カウントダウン、プレイ中、終了後など）の管理。  
* 酸素レベルやスコア、経過時間といった、ゲームの勝敗に関わるグローバルな状態の管理と同期。  
* ゲームルールの具体的な実装（シングルプレイとマルチプレイでの勝利条件の違いなど）をカプセル化する。  
* 他のシステムから発行されたサーバーサイドイベントを購読し、ルールに基づいてGameStateを更新すること。

## **2\. ゲーム進行のステートマシン**

ゲーム全体の流れは、サーバーが一元管理するステートマシンによって制御されます。

### **2.1. ゲームフェーズ (GamePhase)**

ゲームの進行状態を表すEnumを定義します。
```csharp
public enum GamePhase  
{  
    WaitingForPlayers, // プレイヤーの接続待ち（マルチプレイ用）  
    Countdown,         // ゲーム開始前のカウントダウン  
    Playing,           // ゲームプレイ中  
    Finished,          // ゲーム終了（リザルト表示へ）  
}
```

### **2.2. GameManagerによる状態同期**

GameManagerは、現在のゲームフェーズをNetworkVariable\<GamePhase\>として保持します。サーバーがこの値を変更すると、その変更は自動的に全クライアントに同期されます。クライアントは、このフェーズの変更をOnValueChangedイベントで検知し、UIの表示切り替えやプレイヤーの入力制御などを行います。

## **3\. ゲームルールのカプセル化 (ストラテジーパターン)**

シングルプレイとマルチプレイで異なるゲームルール（特に終了条件）を柔軟に扱うため、ストラテジーパターンを採用します。

### **3.1. IGameModeStrategy.cs (Interface)**

ゲームの「ルールブック」の役割を担うインターフェースです。

* **bool IsGameOver(IGameStateReader gameState)**: 現在のゲーム状態で、ゲームが終了条件を満たしているかを判定します。  
* **GameResult CalculateResult(IGameStateReader gameState)**: ゲーム終了時に、勝者や最終スコアなどのリザルト情報を計算します。

### **3.2. 具体的なStrategyクラス**

* **SinglePlayerStrategy.cs**: プレイヤー自身の酸素が0になった場合にゲームオーバーと判定します。  
* **MultiPlayerStrategy.cs**: いずれかのプレイヤーがゲームオーバー状態になった場合に、ゲーム全体を終了と判定します。

GameManagerは、ゲーム起動モード（シングル/マルチ）に応じて、適切なStrategyインスタンスをDIなどで受け取り、ルールの判定をそのStrategyに完全に委譲します。これにより、GameManager自身は具体的なルールを知る必要がなくなります。

## **4\. サーバーのメインゲームループ**

GameManagerは、サーバー上でコルーチンとしてメインゲームループを実行します。このループは、GamePhaseステートマシンに基づいて進行します。

1. **WaitingForPlayersフェーズ**: 必要な数のプレイヤーが接続するまで待機します。  
2. **Countdownフェーズ**: ゲーム開始前に数秒間のカウントダウンを行います。この間、プレイヤーの操作は無効化されます。  
3. **Playingフェーズ**:  
   * このフェーズの間、GameManagerは時間経過による酸素の自然減少などの定常的な状態更新を行います。  
   * 毎フレーム、IGameModeStrategy.IsGameOver()を呼び出し、終了条件をチェックします。  
   * 他のシステム（LevelManagerなど）からのサーバーサイドイベントを購読し、スコア加算などの状態変更を非同期に実行します。  
4. **Finishedフェーズ**:  
   * IsGameOver()がtrueを返すと、このフェーズに移行します。  
   * IGameModeStrategy.CalculateResult()を呼び出し、リザルト情報を確定させます。  
   * 確定したリザルトをクライアントに通知し、必要に応じて**Unity Gaming Services (Leaderboards)** などの外部サービスにスコアを送信します。

### **全体のドキュメント:**　
[./README.md](../../../README.md)
### **関連ドキュメント:**
* [../Player/Player-Design.md](../Player/Player-Design.md)  
* [../UI/UI-Design.md](../UI/UI-Design.md)