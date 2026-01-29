using RiotProxy.Core.Entities;

namespace RiotProxy.Core.Interfaces;

public interface ITeamRoleResponsibilitiesRepository
{
    Task UpsertAsync(TeamRoleResponsibility r);
    Task<IList<TeamRoleResponsibility>> GetByMatchAsync(string matchId);
}

