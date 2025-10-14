const { CurrenciesApi } = require("@unity-services/economy-2.2");

module.exports = async ({ context, params, logger }) => {
    const { projectId, serviceToken } = context;
    const { targetPlayerId } = params;

    const currencyApi = new CurrenciesApi({ accessToken: serviceToken });

    try {
        const response = await currencyApi.getPlayerCurrencies({
            projectId: projectId,
            playerId: targetPlayerId
        });
        
        const ratingCurrency = response.data.results.find(c => c.currencyId === "RATING");

        if (ratingCurrency) {
            return { Rating: ratingCurrency.balance };
        }
    } catch (error) {
        if(logger) logger.error(`[GetRating] Failed to get currency for player ${targetPlayerId}: ${error.message}`);
        return { Rating: 1500 };
    }

    // Return default if currency is not found after a successful API call
    return { Rating: 1500 }; 
};
