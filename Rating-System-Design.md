# レート・ランキングシステム新設計書

## 1. はじめに

本ドキュメントは、プレイヤーのレートとランキングを管理するための新しいシステム設計を定義する。当初Cloud Saveでの実装を試みたが、複数の技術的課題に直面した。その反省を踏まえ、Unity Gaming Services (UGS) の **Economy** および **Leaderboards** サービスを全面的に採用する、より堅牢で公式のベストプラクティスに沿ったアーキテクチャへと移行する。

## 2. これまでの実装の振り返り（反省）

Cloud SaveのJavaScript SDKを利用した実装が失敗した原因を分析し、今後のための教訓とする。

*   **根本原因**: 最大の原因は、公式ドキュメントが不足しているブラックボックス化したJavaScript SDKの動作を、推測に頼って解明しようとしたことにある。デバッグで得られる情報と実際の動作が矛盾する（例：`playerId`が文字列であるはずなのに`[object Object]`になる）という不可解な現象に陥り、論理的な問題解決が不可能となった。
*   **教訓１：公式推奨パスを最優先する**: Unityが推奨するC#での実装や、より目的に適合したUGSサービス（Economyなど）への移行をもっと早い段階で検討・判断すべきだった。
*   **教訓２：状況証拠を尊重する**: 「Cloud Saveで正しいのか？」というアーキテクチャへの疑問や、「`playerId`の扱いは？」といった実装への的確な疑問に対し、その重要性をもっと重く受け止め、立ち止まって調査するべきだった。
*   **今後の対策**: 新機能の実装前には、まず公式ドキュメントやサンプルを徹底的に調査し、その機能の実現に最も適したUGSのサービス・設計パターンを特定する。その上で、設計ドキュメントを作成し、合意が取れてから実装に着手する。

## 3. 新アーキテクチャ概要

*   **レート管理**: プレイヤーのレート値は、**Economy**サービスの**通貨 (Currency)** として管理する。これにより、サーバーからの権限に基づいた、安全でトランザクショナルな数値の更新が可能になる。
*   **ランキング管理**: プレイヤーのレートに基づいたランキングは、**Leaderboards**サービスで実現する。スコアの送信はサーバーから行い、クライアントはランキングの閲覧のみを行う。
*   **サーバーロジック**: 上記の操作はすべて、**Cloud Code**スクリプト（JavaScript）を通じて、サーバーサイドで実行される。

## 4. Unity Dashboard 設定手順

### 4.1. Economy設定

1.  Unity Dashboardの `LiveOps` > `Economy` に移動する。
2.  `Currency` タブを選択し、「Add new currency」ボタンを押す。
3.  以下の通りに新しい通貨を作成する。
    *   **ID**: `RATING`
    *   **Initial balance**: `1500` （新規プレイヤーの初期レート）
    *   **Max balance**: （任意、例えば `10000`）

### 4.2. Leaderboards設定

1.  Unity Dashboardの `LiveOps` > `Leaderboards` に移動する。
2.  「Create leaderboard」ボタンを押す。
3.  以下の通りに新しいリーダーボードを作成する。
    *   **ID**: `RATING_LEADERBOARD`
    *   **Sort Order**: `High to Low` （レートが高い順）
    *   **Tiering**: `Disabled` （または必要に応じて設定）

## 5. Cloud Code 設計

既存の`LoadPlayerData.js`, `SavePlayerData.js`は廃止し、以下の3つの新しいスクリプトを作成・デプロイする。

### 5.1. `UpdateRatings.js`

試合終了後にサーバーから呼び出され、両プレイヤーのレートを更新し、ランキングにスコアを送信する。

**パラメータ:**
*   `winnerId` (string)
*   `loserId` (string)
*   `newWinnerRating` (number)
*   `newLoserRating` (number)

**コード:**
```javascript
const { CurrenciesApi } = require("@unity-services/economy-2.2");
const { LeaderboardsApi } = require("@unity-services/leaderboards-1.1");

module.exports = async ({ context, params }) => {
    const { projectId, serviceToken } = context;
    let { winnerId, loserId, newWinnerRating, newLoserRating } = params;

    // Ensure ratings do not fall below zero
    if (newWinnerRating < 0) {
        newWinnerRating = 0;
    }
    if (newLoserRating < 0) {
        newLoserRating = 0;
    }

    const currencyApi = new CurrenciesApi({ accessToken: serviceToken });
    const leaderboardsApi = new LeaderboardsApi({ accessToken: serviceToken });

    // --- 1. Economy Update ---
    const winnerBalanceRequest = { balance: newWinnerRating };
    await currencyApi.setPlayerCurrencyBalance({
        projectId: projectId,
        playerId: winnerId,
        currencyId: "RATING",
        currencyBalanceRequest: winnerBalanceRequest
    });

    const loserBalanceRequest = { balance: newLoserRating };
    await currencyApi.setPlayerCurrencyBalance({
        projectId: projectId,
        playerId: loserId,
        currencyId: "RATING",
        currencyBalanceRequest: loserBalanceRequest
    });

    // --- 2. Leaderboard Update ---
    const leaderboardId = "RATING_LEADERBOARD";
    await leaderboardsApi.addLeaderboardPlayerScore(projectId, leaderboardId, winnerId, { score: newWinnerRating });
    await leaderboardsApi.addLeaderboardPlayerScore(projectId, leaderboardId, loserId, { score: newLoserRating });

    return { success: true };
};
```

### 5.2. `GetRating.js`

クライアントから呼び出され、指定したプレイヤーの現在のレートを取得する。

**パラメータ:**
*   `targetPlayerId` (string)

**コード:**
```javascript
const { CurrenciesApi } = require("@unity-services/economy-2.2");

module.exports = async ({ context, params }) => {
    const { projectId, serviceToken } = context;
    const { targetPlayerId } = params;

    const currencyApi = new CurrenciesApi({ accessToken: serviceToken });

    const response = await currencyApi.getPlayerCurrencies({
        projectId: projectId,
        playerId: targetPlayerId
    });
    
    const ratingCurrency = response.data.results.find(c => c.currencyId === "RATING");

    if (ratingCurrency) {
        return { rating: ratingCurrency.balance };
    }

    // 通貨が見つからない場合は初期値を返す
    return { rating: 1500 }; 
};
```

### 5.3. `GetLeaderboard.js`

クライアントから呼び出され、ランキング情報を取得する。

**パラメータ:**
*   `offset` (number, optional)
*   `limit` (number, optional)

**コード:**
```javascript
const { LeaderboardsApi } = require("@unity-services/leaderboards-1.1");

module.exports = async ({ context, params }) => {
    // このAPIはプレイヤーとして呼び出すため、プレイヤーのaccessTokenを使用
    const { projectId, accessToken } = context;
    const { offset = 0, limit = 20 } = params;

    const leaderboardsApi = new LeaderboardsApi({ accessToken });
    const leaderboardId = "RATING_LEADERBOARD";

    const response = await leaderboardsApi.getLeaderboardScores(projectId, leaderboardId, { offset, limit });

    return response.data;
};
```
