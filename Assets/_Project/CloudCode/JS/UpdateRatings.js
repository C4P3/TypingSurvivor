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