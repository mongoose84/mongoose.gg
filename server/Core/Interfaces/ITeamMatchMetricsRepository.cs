using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

public interface ITeamMatchMetricsRepository
{
    Task UpsertAsync(TeamMatchMetric t);
    Task<TeamMatchMetric?> GetAsync(string matchId, int teamId);
}

