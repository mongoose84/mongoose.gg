namespace RiotProxy.Application.DTOs;

public static class TeamRolePairEffectivenessDto
{
    /// <summary>
    /// Response containing role pair effectiveness data.
    /// </summary>
    public record TeamRolePairEffectivenessResponse(
        IList<RolePairStats> RolePairs,
        RolePairStats? BestPair,
        RolePairStats? WorstPair
    );

    /// <summary>
    /// Statistics for a specific role pair combination.
    /// </summary>
    public record RolePairStats(
        string Role1,
        string Role2,
        int GamesPlayed,
        int Wins,
        double WinRate
    );
}

