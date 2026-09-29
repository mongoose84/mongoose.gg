namespace Mongoose.Api.Core;

public static class GameConstants
{
    /// <summary>
    /// Minimum game duration in seconds for a match to be treated as a real game.
    /// Matches below this threshold are remakes or abandoned games and must be excluded
    /// from both ingestion (MatchDataPersistenceService) and analytics queries.
    /// </summary>
    public const int MinValidGameDurationSec = 300;

    /// <summary>
    /// Summoner's Rift queue ids: ranked solo (420), ranked flex (440), their normal/blind
    /// counterparts (400, 430) and the newer normal queue (490). Used to build a stable "usual"
    /// for the deciding-stat feature and to exclude ARAM, Arena and other non-SR modes.
    /// </summary>
    public static readonly IReadOnlyList<int> SummonersRiftQueueIds = new[] { 400, 420, 430, 440, 490 };
}
