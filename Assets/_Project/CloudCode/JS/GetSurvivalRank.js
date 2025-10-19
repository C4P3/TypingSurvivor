const { LeaderboardsApi } = require("@unity-services/leaderboards-1.1");

module.exports = async ({ context, params }) => {
    const { projectId, playerId, accessToken } = context;
    const leaderboardsApi = new LeaderboardsApi({ accessToken });
    const leaderboardId = "SURVIVAL_TIME_LEADERBOARD";
    
    const playerEntryPromise = leaderboardsApi.getLeaderboardPlayerScore(projectId, leaderboardId, playerId)
        .catch(e => {
            if (e.response && e.response.status === 404) {
                return null;
            }
            throw e;
        });
        
    const leaderboardInfoPromise = leaderboardsApi.getLeaderboardScores(projectId, leaderboardId, 0, 1);

    const [playerEntryResponse, leaderboardInfoResponse] = await Promise.all([
        playerEntryPromise,
        leaderboardInfoPromise
    ]);

    const playerRank = playerEntryResponse ? playerEntryResponse.data.rank : 0;
    
    const totalPlayers = leaderboardInfoResponse.data.total || 0;

    return { playerRank, totalPlayers };
};