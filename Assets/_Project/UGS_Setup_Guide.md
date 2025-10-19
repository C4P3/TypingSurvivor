# Unity Gaming Services (UGS) 設定ガイド

このドキュメントは、本プロジェクトを完全に動作させるために必要な、Unityダッシュボードでのバックエンド設定をまとめたものです。

---

### 1. Authentication (認証)

1.  Unity Dashboardの「Authentication」セクションに移動します。
2.  「Sign-in methods」タブを選択します。
3.  「Anonymous」を有効化(Enable)し、Defaultに設定します。

---

### 2. Cloud Save (クラウドセーブ)

1.  Unity Dashboardの「Cloud Save」セクションに移動します。
2.  Cloud Saveを有効化します。

*注: 新しいレート・ランキングシステムでは、Cloud Saveはサーバーからの権限が必要なデータ管理には使用しません。プレイヤー名など、クライアント側で完結する小規模なデータの保存に利用できます。*

---

### 3. Economy (エコノミー)

プレイヤーのレート値を管理するために使用します。

1.  Unity Dashboardの `LiveOps` > `Economy` に移動します。
2.  `Currency` タブを選択し、「Add new currency」ボタンを押します。
3.  以下の通りに新しい通貨を作成します。
    *   **ID**: `RATING`
    *   **Initial balance**: `1500`
    *   **Max balance**: (任意)

---

### 4. Leaderboards (リーダーボード)

プレイヤーのレートに基づいたランキングを実現するために使用します。

1.  Unity Dashboardの `LiveOps` > `Leaderboards` に移動します。
2.  「Create leaderboard」ボタンを押します。
3.  以下の通りに新しいリーダーボードを作成します。
    *   **ID**: `RATING_LEADERBOARD`
    *   **Sort Order**: `High to Low`

---

### 5. Cloud Code (クラウドコード)

本プロジェクトでは、Cloud Codeとして3つのJavaScriptスクリプトを使用します。以下の手順でそれぞれを登録してください。

#### A. UpdateRatings スクリプトの登録

*   **説明**: 試合終了後、サーバーから呼び出され、両プレイヤーのレートとランキングを更新します。
*   **Script name**: `UpdateRatings`
*   **コード**: プロジェクトのルートにある `UpdateRatings.js` の内容を貼り付けます。
*   **Parameters**:
    *   `winnerId` (Type: `string`)
    *   `loserId` (Type: `string`)
    *   `newWinnerRating` (Type: `number`)
    *   `newLoserRating` (Type: `number`)

#### B. GetRating スクリプトの登録

*   **説明**: クライアントまたはサーバーから呼び出され、指定したプレイヤーの現在のレートを取得します。
*   **Script name**: `GetRating`
*   **コード**: プロジェクトのルートにある `GetRating.js` の内容を貼り付けます。
*   **Parameters**:
    *   `targetPlayerId` (Type: `string`)

#### C. GetLeaderboard スクリプトの登録

*   **説明**: クライアントから呼び出され、ランキング情報を取得します。
*   **Script name**: `GetLeaderboard`
*   **コード**: プロジェクトのルートにある `GetLeaderboard.js` の内容を貼り付けます。
*   **Parameters**:
    *   `offset` (Type: `number`, optional)
    *   `limit` (Type: `number`, optional)

---

### 6. Matchmaker (マッチメーカー)

1.  Unity Dashboardの「Matchmaker」セクションに移動します。

#### キュー(Queue)の作成

「Queues」タブで、以下の2つのキューを作成します。

1.  **Name:** `free-match`
    *   Timeout: `60` seconds
    *   Team size: `2` (または任意)

2.  **Name:** `ranked-match`
    *   Timeout: `60` seconds
    *   Team size: `2`

#### ルール(Rules)の作成

「Rules」タブで、プライベートマッチ用のルールを作成します。これは、「`room_code`というアトリビュートの値が同じチケット同士をマッチングさせる」というルールです。

*   **Name:** `PrivateRoomRule`
*   **Type:** `Attribute Equality`
*   **Attribute:** `room_code`

作成したこのルールを、`ranked-match`キューと`free-match`キューの両方（あるいはプライベートマッチ専用のキュー）に関連付け、チケットの`room_code`アトリビュートが存在する場合にのみ適用されるように設定します。

---

### 5. Game Server Hosting (Multiplay) の詳細設定

ここでは、アップロードしたサーバービルドを実行するための具体的な設定を作成します。

#### ステップ1: ビルドのアップロード

1.  Unityエディタで、ビルド設定(Build Settings)を開きます。
2.  ターゲットプラットフォームを「Linux」、アーキテクチャを「x86_64」に設定します。
3.  **Dedicated Server**にチェックを入れて、サーバービルドを作成します。
4.  Unity Dashboardの **Multiplay > Builds** に移動し、「Upload build」から作成したサーバービルド（zipファイル）をアップロードします。

#### ステップ2: ビルド構成 (Build Configuration) の作成

次に、アップロードしたビルドをどのように実行するかを定義する「ビルド構成」を**2種類**作成します。

1.  **Multiplay > Build Configurations** に移動し、「Create build configuration」をクリックします。
2.  **1つ目（ランクマッチ用）を作成:**
    *   **Build configuration name**: `Ranked Match Config` （など、分かりやすい名前）
    *   **Build**: ステップ1でアップロードしたビルドを選択します。
    *   **Launch parameters**: `-dedicatedServer -gameMode RankedMatch`
    *   **Query type**: `gamedi` を選択します。（Unity Transport for Netcodeが使用）
    *   設定を保存します。
3.  **2つ目（フリーマッチ用）を作成:**
    *   再度「Create build configuration」をクリックします。
    *   **Build configuration name**: `Free Match Config`
    *   **Build**: 同じビルドを選択します。
    *   **Launch parameters**: `-dedicatedServer -gameMode MultiPlayer`
    *   **Query type**: `gamedi` を選択します。
    *   設定を保存します。

#### ステップ3: フリート (Fleet) の作成

フリートは、どの地域のサーバーを、どれくらいの数だけ待機させておくかを定義します。

1.  **Multiplay > Fleets** に移動し、「Create fleet」をクリックします。
2.  **Name**: `Default Fleet` （など、分かりやすい名前）
3.  **Build configuration**: `Ranked Match Config` または `Free Match Config` のどちらかを選択します。（まずはランクマッチ用を選んでおきましょう）
4.  **Scaling settings** (スケーリング設定):
    *   **Min available servers**: `1` （常に最低1台は待機させる）
    *   **Max servers**: `10` （最大10台まで自動で増やす）
5.  **Locations** (地域): サーバーを配置したい地域（例: `Asia Pacific (Tokyo)`) を選択し、サーバー数を設定します。
6.  設定を保存します。

#### ステップ4: Matchmakerとフリートの紐付け

最後に、Matchmakerがマッチを成立させた時に、どのフリート（＝どのサーバー）を起動するかを教えます。

1.  **Matchmaker > Queues** に移動します。
2.  `ranked-match` キューを編集します。
3.  **Destinations** の設定で、ステップ3で作成したフリート (`Default Fleet`など) を指定します。
4.  `free-match` キューについても同様に設定します。（フリーマッチ専用のフリートを別に作成して紐付けることも、同じフリートを共用することも可能です）

---

以上が、現在のコードを動作させるために最低限必要な設定です。
