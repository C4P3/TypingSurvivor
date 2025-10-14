const { CurrenciesApi } = require("@unity-services/economy-2.2");

module.exports = async ({ context, params, logger }) => {
    const { projectId, serviceToken } = context;
    const { targetPlayerId } = params;

    logger.info(`[GetRating] Script started for targetPlayerId: ${targetPlayerId}`);

    const currencyApi = new CurrenciesApi({ accessToken: serviceToken });

    try {
        logger.info(`[GetRating] Calling getPlayerCurrencies for player: ${targetPlayerId}`);
        const response = await currencyApi.getPlayerCurrencies({
            projectId: projectId,
            playerId: targetPlayerId
        });
        
        logger.info(`[GetRating] API response received: ${JSON.stringify(response.data, null, 2)}`);

        const ratingCurrency = response.data.results.find(c => c.currencyId === "RATING");

        if (ratingCurrency) {
            logger.info(`[GetRating] Found 'RATING' currency with balance: ${ratingCurrency.balance}. Returning this value.`);
            return { Rating: ratingCurrency.balance };
        }
        else
        {
            logger.warn(`[GetRating] 'RATING' currency not found for player. Falling back to default value.`);
        }

    } catch (error) {
        logger.error(`[GetRating] An error occurred while fetching currency: ${error.message}`);
        logger.error(`[GetRating] Stack trace: ${error.stack}`);
        logger.info(`[GetRating] Returning default rating (1500) due to error.`);
        return { Rating: 1500 };
    }

    logger.info(`[GetRating] Reached end of script, returning default rating (1500).`);
    return { Rating: 1500 }; 
};
