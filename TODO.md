# TODOリスト

このドキュメントは、Typing Survivorプロジェクトの未実装の機能や改善点を管理するためのタスクリストです。

## Core System

- [ ] **サービスロケーターの完全移行**: `PlayerFacade`内の`ILevelService`の取得を、`FindObjectOfType`から`AppManager`経由にリファクタリングする。
- [ ] **サービスロケーターへのサービス集約**: `ILevelService`と`IGameStateWriter`を`AppManager`に登録し、`ItemService`などが`FindObjectOfType`ではなく`AppManager`経由で参照するように修正する。
- [ ] **PlayerStatusSystemの永続化**: `PlayerStatusSystem`に、Unity Gaming ServicesのCloud Saveなどを利用したセーブ/ロード機能を追加する。

## Typing機能

### 実装待ちタスク
- [ ] **お題提供クラス(WordProvider)の実装**: `TypingState`内でハードコードされているお題("てすと")を、外部のクラス(CSVやScriptableObjectから単語リストを読み込むWordProviderなど)から動的に取得するように変更する。
- [ ] **Builderクラス群のユニットテスト作成**: 今回のリファクタリングで作成した`KanaParser`および`TrieBuilder`の単体テストを作成し、複雑な変換ロジックの品質を保証する。
- [ ] **ローマ字変換テーブルの読み込み**: `convertTable.json`を起動時に読み込み、`TypingChallenge`で利用可能にする仕組みを実装する（`GameConfig`などでの管理を検討）。
- [ ] **お題提供ロジックの実装**: `TypingTextStore`をリファクタリングし、`TypingChallenge`を生成して`TypingManager`に渡す責務を持たせる。
- [ ] **UIの実装**:
    - [ ] `TypingManager`のイベントを購読し、タイピングのお題（日本語、入力済み/未入力ローマ字）を表示するUIを作成する。
    - [ ] `TypingState`の`Enter`/`Exit`で、UIの表示/非表示を制御する。

### 完了済みタスク
- [x] **`TypingState`の更新**: `Player/StateMachine/States/TypingState.cs` が新しい `TypingManager` のAPI（`StartChallenge`など）に対応していないため修正する。
- [x] **`TypingManager`のAPI修正**: `TypingManager.StopTyping()`がprivateになっており`PlayerFacade`から呼び出せずエラーになっている。適切なアクセス修飾子（publicなど）に変更する。
- [x] **`TypingChallenge`クラスの本格実装**:
    - [x] ローマ字変換テーブル（JSONなど）を読み込む機能を実装する。
    - [x] ひらがなからローマ字のTrie（トライ木）を構築し、複数のタイピングパターン（例: "shi", "si"）に対応できるようにする。

## Gameplay Logic

- [ ] **プレイヤーのスポーン位置修正**: プレイヤーがグリッドセルの中央に正しくスポーンするように修正する（現在、4マスの間にスポーンし初回の移動が不自然になる問題）。
- [ ] **連鎖破壊の実装**: ブロックを破壊した際、隣接する同じ色のブロックもまとめて破壊されるようにする。
- [ ] **アイテム取得ロジックの実装**: `PlayerFacade`の移動処理に、アイテムタイルを踏んだ際に`IItemService.AcquireItem`を呼び出すロジ-ックを追加する。

## Item機能

- [x] **アイテムシステムの基本実装**: アイテムのScriptableObject定義、取得ロジック、`IItemEffect`ストラテジーパターンの実装を行う。
- [ ] **全アイテムエフェクトの実装**: `Item-Effect-List.md` に記載されている未実装のアイテム効果（Star, Rocket, 妨害系など）を実装する。

## その他

- [ ] **エフェクトとサウンド**:
    - [ ] ブロック破壊時のパーティクルエフェクトやサウンドを追加する。
    - [ ] タイピング成功/失敗時のサウンドを追加する。
- [ ] **コードクリーンアップ**:
    - [ ] `PlayerInput`の古い設計（`EnableTypingInput`など）を削除し、単一アクションマップの設計思想を徹底させる。
    - [ ] 各クラスにSummaryコメントを追加する。
