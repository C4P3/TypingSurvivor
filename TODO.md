# TODOリスト

このド-キュメントは、Typing Survivorプロジェクトの未実装の機能や改善点を管理するためのタスクリストです。

## Core System

- [ ] **サービスロ-ケーターの完全移行**: `PlayerFacade`内の`ILevelService`の取得を、`FindObjectOfType`から`AppManager`経由にリファクタリングする。
- [ ] **サービスロケーターへのサービス集約**: `ILevelService`と`IGameStateWriter`を`AppManager`に登録し、`ItemService`などが`FindObjectOfType`ではなく`AppManager`経由で参照するように修正する。
- [ ] **PlayerStatusSystemの永続化**: `PlayerStatusSystem`に、Unity Gaming ServicesのCloud Saveなどを利用したセーブ/ロード機能を追加する。

## Typing機能

- [ ] **UIの実装**:
    - [ ] タイピングのお題（ひらがな、ローマ字）を表示するUIを作成する。
    - [ ] 入力中のテキストを色分け（正解/未入力）して表示するUIを作成する。
    - [ ] `TypingState`の`Enter`/`Exit`で、UIの表示/非表示を制御する。
- [ ] **お題の動的生成**:
    - [ ] 破壊対象のブロックの種類に応じて、`TypingChallenge`を動的に生成するロジックを実装する（例: `WordProvider`クラスの作成）。
    - [ ] `TypingState`の`Enter`で、`NetworkTypingTargetPosition`を元にお題を取得し、`TypingManager`に渡すようにする。
- [ ] **`TypingChallenge`クラスの本格実装**:
    - [ ] ローマ字変換テーブル（JSONなど）を読み込む機能を実装する。
    - [ ] ひらがなからローマ字のTrie（トライ木）を構築し、複数のタイピングパターン（例: "shi", "si"）に対応できるようにする。

## Gameplay Logic

- [ ] **プレイヤーのスポーン位置修正**: プレイヤーがグリッドセルの中央に正しくスポーンするように修正する（現在、4マスの間にスポーンし初回の移動が不自然になる問題）。
- [ ] **連鎖破壊の実装**: ブロックを破壊した際、隣接する同じ色のブロックもまとめて破壊されるようにする。
- [ ] **アイテム取得ロジックの実装**: `PlayerFacade`の移動処理に、アイテムタイルを踏んだ際に`IItemService.AcquireItem`を呼び出すロジックを追加する。

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
