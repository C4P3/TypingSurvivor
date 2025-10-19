const { LeaderboardsApi } = require("@unity-services/leaderboards-1.1");

module.exports = async ({ context, params }) => {
    const { projectId, playerId, accessToken } = context;
    const { survivalTime } = params;

    const leaderboardsApi = new LeaderboardsApi({ accessToken });
    const leaderboardId = "SURVIVAL_TIME_LEADERBOARD";

    // Leaderboard scores are stored as integers, so we multiply by 100 for precision.
    const scoreAsInt = Math.round(survivalTime * 100);

    await leaderboardsApi.addLeaderboardPlayerScore(projectId, leaderboardId, playerId, { score: scoreAsInt });

    return { success: true };
};