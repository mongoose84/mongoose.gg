using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

public interface ITeamObjectivesRepository
{
    Task UpsertAsync(TeamObjective t);
    Task<TeamObjective?> GetAsync(string matchId, int teamId);
}

