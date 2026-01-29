using RiotProxy.Core.Entities;

namespace RiotProxy.Core.Interfaces;

public interface ITeamMatchMetricsRepository
{
    Task UpsertAsync(TeamMatchMetric t);
    Task<TeamMatchMetric?> GetAsync(string matchId, int teamId);
}

