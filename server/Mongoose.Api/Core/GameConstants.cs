namespace Mongoose.Api.Core;

public static class GameConstants
{
    /// <summary>
    /// Minimum game duration in seconds for a match to be treated as a real game.
    /// Matches below this threshold are remakes or abandoned games and must be excluded
    /// from both ingestion (MatchDataPersistenceService) and analytics queries.
    /// </summary>
    public const int MinValidGameDurationSec = 300;
}
