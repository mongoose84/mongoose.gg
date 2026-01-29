using RiotProxy.Core.Entities;

namespace RiotProxy.Core.Interfaces;

public interface ITeamObjectivesRepository
{
    Task UpsertAsync(TeamObjective t);
    Task<TeamObjective?> GetAsync(string matchId, int teamId);
}

