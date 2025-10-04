
// RatingModule.js

/**
 * Saves player data to Cloud Save.
 * This function is designed to be used as a Unity Cloud Code module.
 *
 * @param {object} params - The parameters for the function.
 * @param {object} params.context - The execution context, contains services.
 * @param {string} params.playerId - The ID of the player whose data is being saved.
 * @param {string} params.playerDataKey - The key for the data.
 * @param {any} params.playerData - The data object to save.
 */
async function SavePlayerData({ context, playerId, playerDataKey, playerData }) {
    const { services } = context;
    const data = { [playerDataKey]: playerData };
    // Use the server-authoritative API to set the item.
    await services.cloudSave.setItems(playerId, data);
}

/**
 * Loads player data from Cloud Save.
 * This function is designed to be used as a Unity Cloud Code module.
 *
 * @param {object} params - The parameters for the function.
 * @param {object} params.context - The execution context, contains services.
 * @param {string} params.playerId - The ID of the player whose data is being loaded.
 * @param {string} params.playerDataKey - The key for the data to load.
 * @returns {Promise<any>} The loaded data.
 */
async function LoadPlayerData({ context, playerId, playerDataKey }) {
    const { services, common } = context;
    const keys = [playerDataKey];
    const result = await services.cloudSave.getItems(playerId, keys);

    // Check if the requested key exists in the result
    if (result && result.hasOwnProperty(playerDataKey)) {
        return result[playerDataKey];
    }

    // If the key is not found, throw a specific error that the client can handle.
    throw new common.CloudCodeError(
        'PLAYER_DATA_NOT_FOUND', // A custom error name
        { details: `No data found for key: ${playerDataKey}` }
    );
}

module.exports = {
    SavePlayerData,
    LoadPlayerData
};
