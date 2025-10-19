# プロジェクト概要
[https://github.com/TypingSurvivor-2025-ynu-pro2/TypingSurvivor](https://github.com/TypingSurvivor-2025-ynu-pro2/TypingSurvivor)
[https://github.com/C4P3/ynu-pro2](https://github.com/C4P3/ynu-pro2/blob/main/README.md)
の再設計を行っています。

# 開発の詳細
[Assets\_Project\README.md](.\Assets\_Project\README.md)を参照してください。

# 実行方法 (How to Run)

**重要:** このプロジェクトは、必ず `Assets/_Project/Scenes/App.unity` シーンから起動する必要があります。

`App.unity` は、ゲーム全体で必要となる永続的なオブジェクト (`AppManager` など) を初期化する役割を担っています。他のシーンから直接起動すると、これらのオブジェクトが存在しないためエラーが発生します。

**ビルド設定の確認:**
`File > Build Settings` を開き、`Scenes In Build` のリストの**一番上 (インデックス 0)** に `App.unity` が設定されていることを確認してください。

# 開発タスク
[TODO.md](./Assets/_Project/TODO.md)を参照してください。

# 開発者向けTIPS (Developer Tips)

## マルチプレイのテスト方法

タイトル画面には、UGSの匿名認証プロファイルを切り替える機能が実装されています。これにより、PC上でアプリケーションのビルドを2つ起動し、それぞれ異なるプロファイルでサインインすることで、擬似的にマルチプレイのマッチングテストを行うことが可能です。

1. 1つ目のビルドを起動し、任意の名前（例: `player1`）でプロファイルを作成してログインします。
2. 2つ目のビルドを起動し、タイトル画面の「プロファイル切替」から新しいプロファイル（例: `player2`）を作成してログインします。
3. これで、2人の異なるプレイヤーとしてマッチングを試すことができます。


# 参考文献
    - マルチプレイ
        - あのゲームの作り方Web版
            - NetCode for GameObject
            - Game Server Hosting(Unity)
    - InputSystem
        - https://nekojara.city/unity-input-system-modifier#Button%20with%20one%20modifier%E3%81%AE%E4%BD%BF%E3%81%84%E6%96%B9
