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

---

### 3. Leaderboards (リーダーボード)

1.  Unity Dashboardの「Leaderboards」セクションに移動します。
2.  「Create new leaderboard」をクリックします。
3.  以下の通りに設定します:
    *   **Leaderboard ID**: `high-scores` (コードからこのIDで参照します)
    *   **Leaderboard Name**: `High Scores` (表示名)
    *   **Sort Order**: `Highest to Lowest` (スコアが高い順)
    *   **Aggregation Strategy**: `Best Score` (プレイヤーの自己ベストを保持)
4.  「Save」をクリックして作成します。

---

### 4. Matchmaker (マッチメーカー)

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

### 5. Game Server Hosting (Multiplay)

1.  Unity Dashboardの「Multiplay」セクションに移動します。
2.  Linuxサーバー用のビルドを作成し、ここにアップロードします。
3.  起動パラメータとして、`-dedicatedServer -gameMode RankedMatch` などを設定するビルドプロファイルを作成します。
4.  サーバーのテンプレートとフリートを作成し、Matchmakerと連携させます。

---

以上が、現在のコードを動作させるために最低限必要な設定です。
