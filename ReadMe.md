# 開発の詳細
[Assets\_Project\README.md](Assets\_Project\README.md)を参照してください。

# 実行方法 (How to Run)

**重要:** このプロジェクトは、必ず `Assets/_Project/Scenes/App.unity` シーンから起動する必要があります。

`App.unity` は、ゲーム全体で必要となる永続的なオブジェクト (`AppManager` など) を初期化する役割を担っています。他のシーンから直接起動すると、これらのオブジェクトが存在しないためエラーが発生します。

**ビルド設定の確認:**
`File > Build Settings` を開き、`Scenes In Build` のリストの**一番上 (インデックス 0)** に `App.unity` が設定されていることを確認してください。

# 開発タスク
[TODO.md](./TODO.md)を参照してください。

# 参考文献
    - マルチプレイ
        - あのゲームの作り方Web版
            - NetCode for GameObject
            - Game Server Hosting(Unity)
    - InputSystem
        - https://nekojara.city/unity-input-system-modifier#Button%20with%20one%20modifier%E3%81%AE%E4%BD%BF%E3%81%84%E6%96%B9