# **アイテム効果一覧**

このドキュメントは、旧実装から分析したアイテムの効果を、新しい設計（`IItemEffect`）にどのようにマッピングするかの指針を示すものです。

### **シングルプレイ向けアイテム（強化系）**

| アイテム名 | 効果概要 | 新`IItemEffect`実装案 | 必要なContext |
| :--- | :--- | :--- | :--- |
| **OxygenRecovery** | 酸素を指定量回復する。 | `OxygenHealEffect` | `IGameStateWriter` |
| **MoveSpeedUp** | 移動速度が永続的に上昇する。 | `MoveSpeedUpEffect` | `IPlayerStatusSystemWriter` |
| **Star** | 一定時間、酸素が減らなくなる無敵状態になる。 | `InvincibleEffect` | `IGameStateWriter` |

### **シングルプレイ向けアイテム（ブロック破壊系）**

| アイテム名 | 効果概要 | 新`IItemEffect`実装案 | 必要なContext |
| :--- | :--- | :--- | :--- |
| **Bomb** | 取得した場所を中心に、指定範囲（半径）のブロックを破壊する。 | `ExplodeEffect` | `ILevelService` |
| **Rocket** | プレイヤーの最後の移動方向に、一定距離（例: 10マス）のブロックを一直線に破壊する。 | `DirectionalDestroyEffect` | `ILevelService`, `IPlayerStateReader` (移動方向取得用) |

### **マルチプレイ向けアイテム（妨害系）**

| アイテム名 | 効果概要 | 新`IItemEffect`実装案 | 必要なContext |
| :--- | :--- | :--- | :--- |
| **Poison** | **相手**プレイヤーの酸素を指定量減らす。 | `DamageOpponentOxygenEffect` | `IGameStateWriter` |
| **Thunder** | **相手**プレイヤーを一定時間、移動不能にする。 | `StunOpponentEffect` | `IPlayerStateWriter` (相手の状態変更用) |
| **Unchi** | **相手**プレイヤーの近くにあるアイテムを、破壊不能な「ウンチ」ブロックに変化させる。 | `TransformItemToBlockEffect` | `ILevelService` |
