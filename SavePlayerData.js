const { DataApi, Configuration } = require("@unity-services/cloud-save-1.4");

module.exports = async ({ context, params }) => {
    // APIを正しく初期化
    const apiConfig = new Configuration({
        projectId: context.projectId,
        accessToken: context.accessToken,
    });
    const dataApi = new DataApi(apiConfig);

    // バグを避けるため、context.playerId を直接変数に格納します
    const thePlayerId = context.playerId; 
    
    const request = {
        items: [
            { 
                key: params.playerDataKey, 
                value: params.playerData 
            }
        ]
    };

    // テストランナー（プレイヤー実行）なので、非Protectedのメソッドを呼び出します
    await dataApi.setItemBatch(thePlayerId, request);

    return { success: true, message: "Test runner script succeeded." };
};