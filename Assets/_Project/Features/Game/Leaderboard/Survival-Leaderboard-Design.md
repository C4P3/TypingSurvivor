# **生存時間リーダーボード 設計ドキュメント**

## 1. 目的

このドキュメントは、シングルプレイモードの「生存時間」を記録・集計するためのリーダーボード機能に関するサービス、`ISurvivalLeaderboardService`の設計と、それが依存するUnity Cloud Codeスクリプトの仕様を定義します。

## 2. サービスインターフェース

`ISurvivalLeaderboardService`は、リーダーボードに関するすべての通信をカプセル化します。

```csharp
public interface ISurvivalLeaderboardService
{
    /// <summary>
    /// 指定したスコア（生存時間）をリーダーボードに送信します。
    /// </summary>
    Task SubmitScoreAsync(float survivalTime);

    /// <summary>
    /// 現在のプレイヤーのリーダーボード上のランク情報を取得します。
    /// </summary>
    /// <returns>プレイヤーの順位と、リーダーボードの総エントリー数を含むタプル。</returns>
    Task<(int playerRank, int totalPlayers)> GetPlayerRankAsync();
}
```

## 3. UGS (Unity Dashboard) 設定

このサービスが機能するためには、Unity Dashboardで以下の設定が必要です。

### 3.1. Leaderboardの作成

*   **ID**: `SURVIVAL_TIME_LEADERBOARD`
*   **Sort Order**: `High to Low` (生存時間が長いほど上位)
*   **Tiering**: `Disabled`

### 3.2. Cloud Code スクリプト

以下の2つのJavaScriptファイルをCloud Codeに登録する必要があります。

#### a. `SubmitSurvivalScore.js`

クライアントから呼び出され、認証されたプレイヤーのスコアをリーダーボードに送信します。

*   **パラメータ**:
    *   `survivalTime` (Type: `number`)

*   **コード**:
    ```javascript
    const { LeaderboardsApi } = require("@unity-services/leaderboards-1.1");

    module.exports = async ({ context, params }) => {
        const { projectId, playerId, accessToken } = context;
        const { survivalTime } = params;

        const leaderboardsApi = new LeaderboardsApi({ accessToken });
        const leaderboardId = "SURVIVAL_TIME_LEADERBOARD";

        // Leaderboard scores are stored as integers, so we multiply by 100 for precision.
        const scoreAsInt = Math.round(survivalTime * 100);

        await leaderboardsApi.addLeaderboardPlayerScore(projectId, leaderboardId, playerId, { score: scoreAsInt });

        return { success: true };
    };
    ```

#### b. `GetSurvivalRank.js`

クライアントから呼び出され、認証されたプレイヤーのランクとリーダーボードの総プレイヤー数を返します。

*   **パラメータ**: なし

*   **コード**:
    ```javascript
    const { LeaderboardsApi } = require("@unity-services/leaderboards-1.1");

    module.exports = async ({ context, params }) => {
        const { projectId, playerId, accessToken } = context;
        const leaderboardsApi = new LeaderboardsApi({ accessToken });
        const leaderboardId = "SURVIVAL_TIME_LEADERBOARD";
        
        const playerEntryPromise = leaderboardsApi.getLeaderboardPlayerScore(projectId, leaderboardId, playerId)
            .catch(e => {
                if (e.response && e.response.status === 404) {
                    return null;
                }
                throw e;
            });
            
        const leaderboardInfoPromise = leaderboardsApi.getLeaderboardScores(projectId, leaderboardId, 0, 1);

        const [playerEntryResponse, leaderboardInfoResponse] = await Promise.all([
            playerEntryPromise,
            leaderboardInfoPromise
        ]);

        const playerRank = playerEntryResponse ? playerEntryResponse.data.rank : 0;
        
        const totalPlayers = leaderboardInfoResponse.data.total || 0;

        return { playerRank, totalPlayers };
    };
    ```

