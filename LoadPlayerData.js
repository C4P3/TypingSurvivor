const { DataApi, Configuration } = require("@unity-services/cloud-save-1.4");

module.exports = async ({ context, params }) => {
    // 1. Correctly initialize the API using the Configuration class
    const apiConfig = new Configuration({
        projectId: context.projectId,
        accessToken: context.accessToken,
    });
    const dataApi = new DataApi(apiConfig);

    // 2. Extract playerId from CONTEXT, and other arguments from PARAMS
    const { playerId } = context;
    const { playerDataKey } = params;

    // 3. Call the method with the correct playerId from the context
    const response = await dataApi.getProtectedItems(playerId, [playerDataKey]);

    // 4. Process the response
    if (response.data && response.data.results && response.data.results.length > 0) {
        const item = response.data.results.find(r => r.key === playerDataKey);
        if (item) {
            return item.value;
        }
    }

    // If not found, throw the expected error
    throw new Error("PLAYER_DATA_NOT_FOUND");
};
