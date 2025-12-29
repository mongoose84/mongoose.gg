namespace RiotProxy.Application.DTOs;

public static class TeamChampionCombosDto
{
    /// <summary>
    /// Response containing best team champion combinations.
    /// </summary>
    public record TeamChampionCombosResponse(
        IList<ChampionCombo> Combos,
        int TotalUniqueCompos
    );

    /// <summary>
    /// A specific champion combination the team has played.
    /// </summary>
    public record ChampionCombo(
        IList<ChampionPick> Champions,
        int GamesPlayed,
        int Wins,
        double WinRate
    );

    /// <summary>
    /// A single champion pick within a combination.
    /// </summary>
    public record ChampionPick(
        string PlayerName,
        int ChampionId,
        string ChampionName
    );
}

