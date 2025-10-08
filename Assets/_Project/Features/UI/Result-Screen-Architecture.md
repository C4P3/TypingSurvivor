# リザルト画面アーキテクチャ (リファクタリング後)

リザルト画面のアーキテクチャは、柔軟性を重視し、データハンドリング、アニメーション制御、レイアウトを分離する設計となっています。これは、プロジェクト共通のUIアニメーションシステムを基盤としています。

## 主要コンポーネント

1.  **`ResultScreen.cs` (ファクトリ)**
    - メインのエントリーポイントおよびファクトリとして機能します。
    - ゲームの結果（シングル/マルチ、ランク/フリーなど）に基づき、4つの異なるリザルト画面Prefabのいずれかをインスタンス化します。

2.  **`SinglePlayerResultView.cs` / `MultiplayerResultView.cs` (コントローラー)**
    - これらのスクリプトは、リザルト画面のPrefabにアタッチされます。
    - **責務**: `GameResultDto`からUIにデータを設定し、アニメーションシーケンスを構成すること。
    - フェード用のコルーチンなど、**アニメーションロジックは一切含みません**。
    - `AnimationSequencer`と通信し、再生するアニメーションのステップを制御します (例: `SetStepEnabled("ShowNewRecord", true)`)。

3.  **`AnimationSequencer.cs` (アニメーションドライバー)**
    - Inspectorで定義された一連のアニメーションを実行する、汎用的で再利用可能なコンポーネントです。
    - 様々なUIパネル(`ScreenBase`)のタイミング、順序、フェードを制御します。
    - これにより、C#コードを記述・変更することなく、アニメーションシーケンスを設計・調整できます。

4.  **リザルトPrefab (レイアウト & アニメーションデータ)**
    - ゲームの各結果（`SP_Normal`, `SP_NewRecord`, `MP_Free`, `MP_Ranked`）ごとにユニークなレイアウトを可能にするため、4つの異なるPrefabが存在します。
    - 各Prefabは必要なUI要素を含み、それぞれ独自の`AnimationSequencer`で構成され、固有の表示フローを定義します。
    - 階層的なアニメーションは、入れ子シーケンスのために`SequencedGroupPanel`を使用して実現されます。

## データフロー

1.  `GameManager`がゲームを終了し、`GameResultDto`を付けて`ResultScreen.Show()`を呼び出します。
2.  `ResultScreen.cs`がインスタンス化するべき正しいPrefabを決定します (例: `SP_NewRecord_Result.prefab`)。
3.  新しいPrefabインスタンス上の`SinglePlayerResultView`スクリプトが`GameResultDto`を受け取ります。
4.  `PrepareUIContent()`が呼び出されます。これはテキストフィールドに値を設定し、階層内のすべての`AnimationSequencer`コンポーネントに、条件付きステップ（"ShowNewRecord"など）を有効にするよう指示します。
5.  `ResultView`は、ルートの`AnimationSequencer`で`Play()`を呼び出します。
6.  `AnimationSequencer`は、PrefabのInspectorで設定された定義済みのアニメーションを実行し、パネルを表示・非表示にします。
