# AudioManager 使用箇所の洗い出し (Sound System Refactoring Audit)

このドキュメントは、新しいサウンドシステム（`AudioManager` / `MusicManager`）へのリファクタリング作業に先立ち、既存の `AudioManager` が使用されている箇所を特定し、その目的をまとめたものです。

## 1. BGM/ジングル再生 (新しい `MusicManager` へ移行)

これらの機能は、クロスフェードやスタック管理を行う新しい`MusicManager`が担当します。

-   **`GameManager.cs`**:
    *   **目的:** ゲームのフェーズに応じてBGMやジングルを再生。
    *   **使用箇所:** `PlayBgmClientRpc`, `PlayJingleAndFadeInBgmClientRpc`, `StopBGM`, `ResetAudio`

-   **`GameUIManager.cs`**:
    *   **目的:** 低酸素状態に応じてBGMのピッチを変更。
    *   **使用箇所:** `SetBgmPitch`, `ResetBgmPitch`

-   **`MainMenuManager.cs`**:
    *   **目的:** メインメニューのBGMを再生・停止。
    *   **使用箇所:** `PlayBGM`, `StopBGM`

## 2. 効果音(SFX)再生 (新しい `AudioManager` へ移行)

これらの機能は、`SoundEffectData`に基づいてSFXを再生する新しい`AudioManager`が担当します。

-   **`GameManager.cs`**:
    *   **目的:** ゲーム開始のカウントダウン音を全クライアントで再生。
    *   **使用箇所:** `PlaySfxOnAllClients`

-   **`InGameHUDManager.cs`**:
    *   **目的:** タイピングの打鍵音、成功音、失敗音をローカルで再生。
    *   **使用箇所:** `PlaySfxWithRandomPitch`, `PlaySfx`

-   **`LevelManager.cs`**:
    *   **目的:** ブロック破壊音を、破壊された位置で再生。
    *   **使用箇所:** `PlaySoundAtPointWithRandomPitch`

-   **`BombEffect.cs` (Item Effect)**:
    *   **目的:** 爆弾アイテムの爆発音を再生。
    *   **使用箇所:** `PlaySoundAtPoint`

## 3. 初期化と依存性注入 (修正が必要)

-   **`AppManager.cs`**:
    *   **目的:** `AudioManager`のインスタンスを生成時に初期化。
    *   **対応:** `AudioManager`と`MusicManager`の両方を初期化するように修正が必要です。

-   **`GameSceneBootstrapper.cs`**:
    *   **目的:** `ItemService`などの他のサービスに`AudioManager`のインスタンスを注入。
    *   **対応:** `AudioManager`と`MusicManager`を適切に注入するように修正が必要です。

-   **`ItemService.cs` / `ItemExecutionContext.cs`**:
    *   **目的:** アイテム効果の中から音を再生するために`AudioManager`への参照を保持。
    *   **対応:** SFX再生専用の新しい`AudioManager`を引き続き利用します。
