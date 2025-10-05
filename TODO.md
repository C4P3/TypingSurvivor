# TODOリスト

このドキュメントは、Typing Survivorプロジェクトの未実装の機能や改善点を管理するためのタスクリストです。
タスクは優先度が高いと思われる順に並んでいます。


## 急ぎ
- [ ] **バグ修正**:
    - Failed to delete ticket: The object of type 'TypingSurvivor.Features.UI.Screens.MainMenu.MatchmakingController' has been destroyed but you are still trying to access it.
Your script should either check if it is null or you should not destroy the object. Parameter name: obj
UnityEngine.Debug:LogWarning (object)
TypingSurvivor.Features.Core.Matchmaking.MatchmakingService/<CancelMatchmaking>d__17:MoveNext () (at Assets/_Project/Features/Core/Matchmaking/Scripts/MatchmakingService.cs:144)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder:SetResult ()
Unity.Services.Matchmaker.WrappedMatchmakerService/<DeleteTicketAsync>d__9:MoveNext () (at ./Library/PackageCache/com.unity.services.multiplayer@34def56704ad/Runtime/Matchmaker/SDK/WrappedMatchmakerService.cs:136)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Unity.Services.Matchmaker.Response>:SetResult (Unity.Services.Matchmaker.Response)
Unity.Services.Matchmaker.WrappedMatchmakerService/<TryCatchRequest>d__16`1<Unity.Services.Matchmaker.Tickets.DeleteTicketRequest>:MoveNext () (at ./Library/PackageCache/com.unity.services.multiplayer@34def56704ad/Runtime/Matchmaker/SDK/WrappedMatchmakerService.cs:264)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Unity.Services.Matchmaker.Response>:SetResult (Unity.Services.Matchmaker.Response)
Unity.Services.Matchmaker.Apis.Tickets.TicketsApiClient/<DeleteTicketAsync>d__8:MoveNext () (at ./Library/PackageCache/com.unity.services.multiplayer@34def56704ad/Runtime/Matchmaker/Apis/TicketsApi.cs:150)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Unity.Services.Matchmaker.Http.HttpClientResponse>:SetResult (Unity.Services.Matchmaker.Http.HttpClientResponse)
Unity.Services.Matchmaker.Http.HttpClient/<MakeRequestAsync>d__1:MoveNext () (at ./Library/PackageCache/com.unity.services.multiplayer@34def56704ad/Runtime/Matchmaker/Http/HttpClient.cs:41)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Unity.Services.Matchmaker.Http.HttpClientResponse>:SetResult (Unity.Services.Matchmaker.Http.HttpClientResponse)
Unity.Services.Matchmaker.Http.HttpClient/<CreateWebRequestAsync>d__3:MoveNext () (at ./Library/PackageCache/com.unity.services.multiplayer@34def56704ad/Runtime/Matchmaker/Http/HttpClient.cs:56)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Unity.Services.Matchmaker.Http.HttpClientResponse>:SetResult (Unity.Services.Matchmaker.Http.HttpClientResponse)
Unity.Services.Matchmaker.Http.HttpClient/<CreateHttpClientResponse>d__4:MoveNext () (at ./Library/PackageCache/com.unity.services.multiplayer@34def56704ad/Runtime/Matchmaker/Http/HttpClient.cs:84)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Unity.Services.Matchmaker.Http.HttpClientResponse>:SetResult (Unity.Services.Matchmaker.Http.HttpClientResponse)
Unity.Services.Matchmaker.Http.HttpClient/<>c__DisplayClass4_0/<<CreateHttpClientResponse>b__0>d:MoveNext () (at ./Library/PackageCache/com.unity.services.multiplayer@34def56704ad/Runtime/Matchmaker/Http/HttpClient.cs:81)
System.Threading.Tasks.TaskCompletionSource`1<Unity.Services.Matchmaker.Http.HttpClientResponse>:SetResult (Unity.Services.Matchmaker.Http.HttpClientResponse)
Unity.Services.Matchmaker.Http.UnityWebRequestHelpers/<>c__DisplayClass0_0:<GetAwaiter>b__0 (UnityEngine.AsyncOperation) (at ./Library/PackageCache/com.unity.services.multiplayer@34def56704ad/Runtime/Matchmaker/Http/UnityWebRequestHelpers.cs:34)
UnityEngine.AsyncOperation:InvokeCompletionEvent ()
　- メインメニューシーンに戻って再度マッチングを開始するとマッチングが永遠に終わらず、マッチングをキャンセルしようとすると発生。
- [ ] **フリーマッチでもレート計算しちゃう**:
  - Strategyで分岐を追加。というか、Strategyに計算式があってもいい
---

## 🚀 フェーズ1: ゲームフロー基盤の実装 (完了)

プレイヤーがゲームを開始し、対戦し、再戦するという一連の流れを完全に機能させることができました。
これで、ゲームの基本的なフローが完成しました。次のフェーズに進みます。

---

## 🚀 フェーズ2: マッチング機能の実装

- [x] **Matchmaking機能の設計**:
    - [x] `Matchmaking-Design.md` を作成し、UGSのMatchmakerとRelayを利用したシステムの全体像を設計した。
- [x] **Matchmaking機能の実装**:
    - [x] `MatchmakingService`と`MatchmakingController`を実装し、公開マッチと合言葉マッチのロジックを構築した。
- [ ] **Matchmaking UIの接続**:
    - [ ] Unity Editor上で、`MainMenuManager`と`MatchmakingController`に、実際のUIコンポーネント（ボタン、入力フィールドなど）を接続する。

---

## 🛠️ フェーズ4: オンライン機能の実装

コアなゲーム体験が固まった後、本格的なマルチプレイ体験の構築と、長期的な運用を見据えた改善を行います。

- [ ] **ランクマッチとプライベートマッチの設計・実装**:
    - [ ] `Matchmaking-Design.md` と `Gameplay-Design.md` を更新し、ランクマッチとプライベートマッチのフローを定義する。
    - [ ] `GameModeType` enumに `RankedMatch` を追加する。
    - [ ] ランクマッチの勝敗とレート計算ロジックを持つ `RankedMatchStrategy.cs` を作成する。
    - [ ] Matchmakerサービスを利用したプライベートマッチ（合言葉マッチ）のロジックを `MatchmakingService` と `MatchmakingController` に実装する。

- [x] **Unity Gaming Services連携 (Matchmaker, Hosting)**:
    - [x] UGSの各サービスを導入し、マッチング接続の基盤を構築する。
- [x] **マッチングUIの実装**:
    - [x] MainMenuシーンに「フリーマッチ」「レートマッチ」「合言葉マッチ」の各モードを選択し、マッチング待機を行うUIを実装する。
- [x] **レートシステムの設計・実装**:
    - [x] レートマッチのための内部レート計算、表示、保存（UGS Cloud Saveなど）の仕組みを実装する。
- [x] **合言葉マッチの実装**:
    - [x] プライベートなルームを作成・検索するための合言葉（ルームコード）機能を実装する。
- [x] **UIアニメーションの改善 (CanvasGroup)**: TypingViewなどのUI表示切り替えを、現在のSetActiveからCanvasGroupのalphaを利用したフェードイン/アウトに移行し、より洗練されたユーザー体験を提供する。
- [ ] **PlayerStatusSystemの永続化**: 
    - [ ] `PlayerStatusSystem`に、Unity Gaming ServicesのCloud Saveなどを利用したセーブ/ロード機能を追加する。
- [ ] **コードクリーンアップ**:
    - [ ] **MainMenuManagerの責務分割**: 現在MainMenuManagerに集中している各パネルのロジックを、パネルごとの専用コントローラークラスに分割し、MainMenuManagerは上位のイベントを購読するだけの形にリファクタリングする。
    - [ ] `PlayerInput`の古い設計（`EnableTypingInput`など）を削除し、単一アクションマップの設計思想を徹底させる。
    - [ ] 各クラスにSummaryコメントを追加する。
- [ ] **マッチング失敗時の復帰バグ修正**: マッチング開始直後に通信が失敗した場合、キャンセル処理も失敗してUIが操作不能になる問題を修正する。通信状態に関わらず、安全に前の画面に戻れるようにする。

---

## 💡 フェーズ5: 将来的なアイデア
- [ ] **アイテムの連鎖効果の設計**
    - [ ] 爆弾などの一部のアイテムについては、爆発により、他の爆弾を爆発させることができるようにしたい。同時に、この連鎖の影響を受けないアイテムと影響を受けるアイテムを上手く管理したい。
- [ ] **アイテムレーダーや通った通路のミニマップ導入**:
    - [ ] **アイテムレーダー**: 小さい小窓などで四隅の端に、アイテムの存在を示すミニマップ機能を実装し、追加する。
    - [ ] **通った通路のミニマップ**: 近くを通ると、ミニマップの画面に対して少し明るめの色で道があると示すマップの作製

- [ ] **クロススクリーンエフェクトの実装**
    - [ ] アイテム効果をよりダイナミックに表現するため、画面分割の境界を越えて移動するエフェクトを実装する。
    - [ ] **例**: サンダーアイテムを取得したプレイヤーの画面上部から光線が発射され、相手プレイヤーの画面上部からその光線が降ってきて着弾する、といった一連の演出。
    - [ ] **技術的課題**: 各プレイヤーのカメラは異なる`Render Texture`や`Viewport`を持つため、複数のカメラをまたいで描画されるエフェクトには、UIレイヤーや別のカメラを使った特殊な実装が必要になる可能性が高い。

---

## ✅ 完了済みタスク (Completed)

- [x] **UIナビゲーションシステムの本格実装**:
    - [x] `MainMenuManager`の責務を`UIFlowCoordinator`に集約し、`UI-Design.md`に記載された画面フローを完全に実装した。
    - [x] `UIFlowCoordinator`にステートマシンを実装し、ランキング、設定、ショップなど全ての画面への遷移ロジックを実装した。
    - [x] ゲーム開始ロジックを`AppManager`に集約し、UIの責務と明確に分離した。
- [x] **サウンドとVFXのリッチ化**:
    - [x] **コアVFXシステムの機能拡張**: `EffectManager`を拡張し、持続・追従エフェクトと指向性エフェクトを実装した。
    - [x] **各種IDの追加**: `AudioRegistry`と`VFXRegistry`に新しいアイテム用のIDを追加した。
    - [x] **アイテムエフェクトの実装と修正**: スター、サンダー、ロケット、回復、加速、毒、うんちアイテムのエフェクトとサウンドを、拡張された新システムを使って実装・修正した。

- [x] **低酸素時の演出を実装**:
    - [x] サーバー(`GameManager`)が低酸素状態を検知し、ClientRpcで全クライアントに通知する仕組みを実装した。
    - [x] クライアント(`GameUIManager`)が通知を受け、カスタムポストエフェクトシェーダーを用いて、対象プレイヤーの画面を赤く点滅させる演出を実装した。
    - [x] 低酸素状態のプレイヤー本人のみ、BGMのピッチが上昇する演出を実装した。
    - [x] ゲーム終了時に全ての演出がリセットされるようにした。
- [x] **多様なマップ生成アルゴリズムの実装**:
    - [x] `TerrainPreset` ScriptableObjectを導入し、ジェネレーター間で地層設定を共有できるようにした。
    - [x] `FbmCaveGenerator`, `CellularAutomataGenerator`, `VoronoiGenerator`, `RandomWalkGenerator`, `BspDungeonGenerator`を実装した。
- [x] **パーリンノイズマップジェネレーターのバグ修正**:
    - [x] スポーン地点が確保されない問題を`LevelManager`の責務として解決し、ジェネレーターの安定性を向上させた。
    - [x] `PerlinNoiseMapGenerator`のアルゴリズムを、より直感的で安定したものに修正した。
- [x] **アイテムスポーンロジックの改善**: アイテムがブロックを上書きして生成されるように`LevelManager`のロジックを修正した。
- [x] **エフェクトとサウンドの基盤を実装**:
    - [x] `Effect-and-Audio-Design.md`に基づき、永続的なコアサービスとして`AudioManager`と`EffectManager`を実装した。
    - [x] IDとアセットを管理する`AudioRegistry`と`VFXRegistry` (ScriptableObject) を作成した。
    - [x] `CameraManager`がローカルプレイヤーの`AudioListener`のみを有効化するよう修正し、画面分割時のサウンド再生問題を解決した。
- [x] **単一エフェクトの実装（ボム）**:
    - [x] 基盤システムを使い、ボムアイテムの爆発エフェクトとサウンドを実装し、サーバーからクライアントへ再生させるフローを確立した。
    - [x] エフェクトの再生スケールを動的に変更する仕組みを実装した。
- [x] **タイルとのインタラクションをリッチにする**: `TileInteractionType` enumを導入し、破壊不能ブロックに衝突した際にタイピングモードに入らず停止するように`PlayerFacade`のロジックを修正した。
- [x] **全アイテムエフェクトの実装**: `Item-Effect-List.md` に記載されている全てのアイテム効果（Rocket, Stun, Poisonなど）を実装した。
- [x] **Unchiアイテムの実装**: 汎用的な破壊不能ブロックシステムを導入し、相手の近くのアイテム1つを破壊不能ブロックに変化させる効果を実装した。
- [x] **ゲームモード別のアイテム出現ロジック実装**: `IItemPlacementStrategy`をゲームモードごとに切り替えられるようにリファクタリングし、シングルプレイとマルチプレイで異なるアイテム出現ルールを適用できるようにした。
- [x] **Star（無敵）アイテムの実装**: PlayerStatusSystemを拡張し、ダメージ軽減率ステータスを利用して無敵効果を実装した。
- [x] **アイテム生成ロジックの改善**: IItemPlacementStrategyが抽選ロジックの全責任を負うようにリファクタリングし、重み付きランダム抽選を実装した。
- [x] **タイピングUIの実装**: InGameHUDにタイピングの進捗を表示するTypingViewを実装した。
- [x] **連鎖破壊の実装**: `LevelManager`に連鎖破壊ロジックを実装し、破壊APIを`DestroyConnectedBlocks`と`DestroyBlockAt`に分離して拡張性を向上させた。
- [x] **再戦・スタート時のキャラクター位置を同期**: `PlayerFacade`で移動距離に基づく判定を導入し、スポーン/リスポーン時のワープと通常移動時のなめらかなアニメーションを両立させた。これにより`NetworkVariable`の警告も解消した。
- [x] **クライアント切断時のゲームフローを設計・実装**: 設計ドキュメントを作成し、サーバー側・クライアント側双方の切断検知と、それに伴う安全なゲーム終了/中断処理を実装した。
- [x] **マップ生成システムの刷新**: `MapGenerationRequest`モデルを導入し、`LevelManager`と`GameManager`の関連ロ-ジックをリファクタリングした。
- [x] **`AppManager.GameMode`連携**: `static string`を廃止し、Enumとインスタンスプロパティによる型安全な管理方法にリファクタリングした。
- [x] **ローカル専用サーバーの接続問題を解決**: クライアントが`127.0.0.1`に正しく接続できるように`MainMenuManager`を修正し、`AppManager`に起動ロジックを集約した。
- [x] **マルチプレイの酸素管理**:
    - [x] `PlayerData`に個別の酸素レベルを追加し、`GameManager`が各プレイヤーの酸素を管理するようにした。
    - [x] `MultiPlayerStrategy`が生存プレイヤー数に基づいてゲームオーバーを判定するようにした。
    - [x] UIが各プレイヤーの酸素レベルを正しく表示し、再戦時にリセットされるようにした。
- [x] **再戦ロジックの実装**:
    - [x] `GameManager`がプレイヤーインスタンスを管理し、シーンをリロードせずにゲーム状態をリセットして再戦を開始する、堅牢なゲームループを実装した。
    - [x] `PlayerFacade`にリスポーン用のメソッドを追加し、`GameManager`が`LevelManager`と連携してプレイヤーを再配置する責務の分離されたフローを確立した。
- [x] **`GameUIManager`によるUI状態管理の実装**:
    - [x] `Game`シーン全体のUIの表示/非表示を`GamePhase`に応じて一元管理する`GameUIManager`クラスを作成した。
    - [x] リザルト画面のシンプルなUIコンポーネントを作成し、`GameUIManager`が制御できるようにした。
    - [x] `GameUIManager`がUIイベントを購読し、`GameManager`に通知するフローを実装した。
- [x] **シーン遷移戦略の策定**: 単一Gameシーン、高速再戦、動的画面分割などのゲームフロー全体を設計した。
- [x] **サーバー起動フローの改善**: コマンドライン引数でゲームモードを指定し、サーバーを直接起動できるように`ServerStartup.cs`を修正した。
- [x] **メインメニューのシーン遷移修正**: UIボタンからHost/Client/Serverとして正常にGameシーンへ遷移できるように`MainMenuManager`を修正した。
- [x] **初期化フローの改善**: `App`シーンですぐに`MainMenu`をロードし、コアサービスの初期化を非同期で行うイベント駆動のフローに修正した。
- [x] **ゲームモード戦略の実装**: `GameManager`がゲームモードに応じて`SinglePlayerStrategy`と`MultiPlayerStrategy`を切り替えられるようにした。
- [x] **プレイヤーのスポーン位置修正**: Netcodeの自動スポーンを無効化し、GameManagerがLevelManagerと連携して、グリッド中央に手動でスポーンさせるように修正した。
- [x] **お題提供クラス(WordProvider)の実装**: `TypingState`内でハードコードされているお題を、外部のクラスから動的に取得するように変更した。
- [x] **依存性注入(DI)の改善**: `AppManager`をサービスロケーターとし、`GameSceneBootstrapper`がシーンの依存性を注入するComposition Rootとして機能するようにリファクタリングした。
- [x] **PlayerStatusSystemの設計と実装の改修**: 循環参照を解消するため、`PlayerStatusSystem`をCore機能に移動し、一時効果を扱えるように再設計・実装した。
- [x] **ローマ字変換テーブルの読み込み**: `convertTable.json`を起動時に読み込み、`TypingChallenge`で利用可能にする仕組みを実装した。
- [x] **`TypingChallenge`クラスの本格実装**: Trie木を構築し、複数のタイピングパターンに対応できるようにした。
- [x] **アイテムシステムの基本実装**: アイテムのScriptableObject定義、取得ロジック、`IItemEffect`ストラテジーパターンの実装を行った。
- [x] **アイテム取得ロジックの実装**: `PlayerFacade`の移動処理に、アイテムタイルを踏んだ際に`IItemService.AcquireItem`を呼び出すロジックを追加した。
- [x] **`CameraManager`の実装**:
    - [x] プレイヤー数に応じてカメラのViewportを動的に変更し、画面分割を実現する。
    - [x] 各カメラが正しいプレイヤーを追従するようにする。
    - [x] 各プレイヤーのUI Canvasを、対応するカメラに割り当てる。