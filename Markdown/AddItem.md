# 新しいアイテム「ロケット🚀」の追加手順
- この仕組みを使うと、アイテム追加は以下のステップで完了します。

1. 【効果の作成】 (※必要な場合のみ)
    - 新しい「ロケット」の効果（指定方向に直進して壁まで破壊）が必要だとします。プログラマーが RocketEffect.cs を作成します。

    - 置き場所: Features/Game/Items/Scripts/Effects/

2. 【効果アセットの作成】
    - Unityエディタのメニュー (Assets > Create > ...) から、先ほど作成した RocketEffect のアセット (.asset ファイル) を作成します。破壊する範囲などをインスペクターで設定できるようにしておくと便利です。

    - 置き場所: Features/Game/Items/Data/Effects/ (新設)

3. 【アイテムデータの作成】
    - Unityエディタのメニューから ItemData のアセット Rocket.asset を作成します。

    - 置き場所: Features/Game/Items/Data/

4. 【インスペクターで設定】
    - 作成した Rocket.asset を選択し、インスペクターで以下を設定します。

    - Item Name: "ロケット"

    - Icon: ロケットのアイコン画像を指定

    - Item Tile: タイルマップ用のタイルを指定

    - Effect: 手順2で作成した RocketEffect.asset をドラッグ＆ドロップ！

5. 【アイテムDBへ登録】
    - Settings フォルダにある ItemRegistry.asset を選択し、アイテムリストに今作った Rocket.asset を追加します。