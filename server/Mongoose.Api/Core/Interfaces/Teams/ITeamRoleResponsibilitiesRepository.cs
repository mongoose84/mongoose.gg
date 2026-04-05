using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

public interface ITeamRoleResponsibilitiesRepository
{
    Task UpsertAsync(TeamRoleResponsibility r);
    Task<IList<TeamRoleResponsibility>> GetByMatchAsync(string matchId);
}

