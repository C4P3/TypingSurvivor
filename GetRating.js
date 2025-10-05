const { EconomyApi } = require("@unity-services/economy-2.2");

module.exports = async ({ context, params }) => {
    const { projectId, serviceToken } = context;
    const { targetPlayerId } = params;

    const economyApi = new EconomyApi({ accessToken: serviceToken });

    const response = await economyApi.getPlayerCurrencies(projectId, targetPlayerId);
    
    const ratingCurrency = response.data.results.find(c => c.currencyId === "RATING");

    if (ratingCurrency) {
        return { rating: ratingCurrency.balance };
    }

    // 通貨が見つからない場合は初期値を返す
    return { rating: 1500 }; 
};
