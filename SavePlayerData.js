const axios = require("axios-1.6");

module.exports = async ({ context, params }) => {
    const { projectId, playerId, accessToken } = context;
    let { playerDataKey, playerData } = params;

    // The Test Runner wraps string parameters in extra quotes. 
    // We parse them to get the actual string value.
    // This should be safe for the real C# client which will send a raw string.
    try {
        playerDataKey = JSON.parse(playerDataKey);
    } catch (e) { /* Ignore if it's not a JSON-formatted string */ }

    // The Test Runner also stringifies object parameters.
    if (typeof playerData === 'string') {
        try {
            playerData = JSON.parse(playerData);
        } catch (e) { /* Ignore if it's just a plain string */ }
    }

    // The correct body for a single item save is a single object.
    const body = {
        "key": playerDataKey,
        "value": playerData
    };

    const url = `https://cloud-save.services.api.unity.com/v1/data/projects/${projectId}/players/${playerId}/items`;

    const response = await axios.post(url, body, {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${accessToken}`
        }
    });

    if (response.status < 200 || response.status >= 300) {
        throw new Error(`Failed to save data: ${response.status} - ${JSON.stringify(response.data)}`);
    }

    return { success: true };
};
