const { LeaderboardsApi } = require("@unity-services/leaderboards-1.1");

module.exports = async ({ context, params }) => {
    // このAPIはプレイヤーとして呼び出すため、プレイヤーのaccessTokenを使用
    const { projectId, accessToken } = context;
    const { offset = 0, limit = 20 } = params;

    const leaderboardsApi = new LeaderboardsApi({ accessToken });
    const leaderboardId = "RATING_LEADERBOARD";

    const response = await leaderboardsApi.getLeaderboardScores(projectId, leaderboardId, { offset, limit });

    return response.data;
};
