const { EconomyApi } = require("@unity-services/economy-2.2");
const { LeaderboardsApi } = require("@unity-services/leaderboards-1.1");

module.exports = async ({ context, params }) => {
    const { projectId, serviceToken } = context;
    const { winnerId, loserId, newWinnerRating, newLoserRating } = params;

    // サーバーとして動作するため、プレイヤーのトークンではなくサービスアカウントのトークンを使用
    const economyApi = new EconomyApi({ accessToken: serviceToken });
    const leaderboardsApi = new LeaderboardsApi({ accessToken: serviceToken });

    // --- 1. Economyで両プレイヤーのレート（通貨残高）を更新 ---
    const winnerBalanceRequest = {
        currencyId: "RATING",
        balance: newWinnerRating
    };
    const loserBalanceRequest = {
        currencyId: "RATING",
        balance: newLoserRating
    };

    await economyApi.setPlayerCurrencyBalance(projectId, winnerId, "RATING", winnerBalanceRequest);
    await economyApi.setPlayerCurrencyBalance(projectId, loserId, "RATING", loserBalanceRequest);

    // --- 2. Leaderboardsに両プレイヤーの新しいスコアを送信 ---
    const leaderboardId = "RATING_LEADERBOARD";
    
    await leaderboardsApi.addLeaderboardPlayerScore(projectId, leaderboardId, winnerId, { score: newWinnerRating });
    await leaderboardsApi.addLeaderboardPlayerScore(projectId, leaderboardId, loserId, { score: newLoserRating });

    return { success: true };
};
