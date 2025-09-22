# **タイピング探索ゲーム 再設計ドキュメント**

## **1\. はじめに**

このドキュメント群は、「タイピング探索ゲーム」の再設計に関する全ての設計思想、アーキテクチャ、および詳細な仕様を記録するものです。

このプロジェクトは、シングルプレイの基盤から、将来的なマルチプレイ（協力・対戦）、ローグライクモードといった大規模な拡張にも耐えうる、堅牢で柔軟なアーキテクチャへの刷新を目指します。

**目的:**

* 未来の自分や新しいチームメンバーが、設計の意図を迅速に理解できるようにする。  
* 機能追加や改修の際に、設計思想に立ち返るための「憲法」とする。  
* 各機能が疎結合に連携するクリーンな状態を維持する。

## **2\. 設計ドキュメント一覧**

### **Tier 1: 全体設計 \- まずここから読む**

これらのドキュメントは、プロジェクト全体の設計思想と骨格を定義します。新しいメンバーはまずここから読み進めることを推奨します。

* [**./Architecture-Overview.md**](./Architecture-Overview.md)  
  * **内容**: プロジェクトのコア原則（疎結合、関心の分離）、システム全体の構成図、採用した主要な設計パターンについて概説します。  
* [**./Folder-Structure.md**](./Folder-Structure.md)  
  * **内容**: このプロジェクトで採用するフォルダ構造の全体像と、その設計意図について詳述します。  
* [**./Data-Flow.md**](./Data-Flow.md)  
  * **内容**: 「プレイヤーがアイテムを取得し、UIが更新されるまで」といった、複数の機能をまたがる代表的な処理のシーケンス図を掲載し、システム間の連携を視覚的に解説します。

### **Tier 2: 機能別 詳細設計**

各機能（Feature）の内部実装に関する詳細なドキュメントです。特定の機能について深く知りたい場合に参照してください。

* **ゲームプレイの根幹**  
  * [**./Features/Game/Gameplay/Gameplay-Design.md**](./Features/Game/Gameplay/Gameplay-Design.md): ゲームのルール、勝敗条件、スコアなどを管理するGameManagerとGameState（NetworkVariable群）について。  
  * [**./Features/Game/Player/Player-Design.md**](./Features/Game/Player/Player-Design.md): プレイヤーの入力、状態管理（StateMachine）、ネットワーク同期（Facade）、永続ステータス（StatusSystem）について。  
* **ゲーム世界を構成する要素**  
  * [**./Features/Game/Level/Level-Design.md**](./Features/Game/Level/Level-Design.md): チャンクベースの動的なマップ生成と、NetworkListを用いた状態同期について。  
  * [**./Features/Game/Item/Item-Design.md**](./Features/Game/Item/Item-Design.md): ScriptableObjectとストラテジーパターンを用いた、拡張性の高いアイテムシステムについて。  
* **プレイヤーとのインタラクション**  
  * [**./Features/Game/Typing/Typing-Design.md**](./Features/Game/Typing/Typing-Design.md): データ駆動のローマ字変換、新Input Systemとの連携について。  
  * [**./Features/Game/UI/UI-Design.md**](./Features/UI/UI-Design.md): 疎結合なUI更新を実現するための、画面Managerとコンポーネントの設計について。

では、次に**Tier 1**の各ドキュメントを作成していきます。まずは Architecture-Overview.md から始めましょう。