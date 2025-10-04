const axios = require("axios-1.6");

module.exports = async ({ context, params }) => {
    const { projectId, playerId, accessToken } = context;
    let { playerDataKey } = params;

    // The Test Runner may wrap the string parameter in extra quotes. 
    // We parse it to get the actual string value.
    try {
        playerDataKey = JSON.parse(playerDataKey);
    } catch (e) { /* Ignore if it's not a JSON-formatted string */ }

    const url = `https://cloud-save.services.api.unity.com/v1/data/projects/${projectId}/players/${playerId}/items?keys=${playerDataKey}`;

    const response = await axios.get(url, {
        headers: {
            'Authorization': `Bearer ${accessToken}`
        }
    });

    if (response.status < 200 || response.status >= 300) {
        throw new Error(`Failed to load data: ${response.status} - ${JSON.stringify(response.data)}`);
    }

    const responseData = response.data;

    if (responseData.results && responseData.results.length > 0) {
        return responseData.results[0].value;
    }

    // To align with the C# code, we throw an error that can be caught.
    // The C# side specifically looks for "Not Found" or "PLAYER_DATA_NOT_FOUND".
    throw new Error("PLAYER_DATA_NOT_FOUND");
};