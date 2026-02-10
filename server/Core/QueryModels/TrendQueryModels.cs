namespace Mongoose.Api.Core.QueryModels;

/// <summary>
/// Core layer data models for trend-related queries.
/// These are returned by repositories and mapped to DTOs in endpoints.
/// </summary>

/// <summary>
/// A single data point for the winrate trend.
/// Represents the rolling average winrate at a specific game in the timeline.
/// </summary>
public record WinrateTrendData(
    int GameIndex,
    double WinRate,
    DateTime Timestamp
);

/// <summary>
/// A single data point for the LP trend.
/// Represents LP and rank after each ranked game.
/// </summary>
public record LpTrendData(
    /// <summary>1-indexed game number, oldest to newest</summary>
    int GameIndex,
    /// <summary>LP gained or lost in this game (null if unknown - first game or missing data)</summary>
    int? LpGain,
    /// <summary>Current LP after this game</summary>
    int CurrentLp,
    /// <summary>Rank string after this game (e.g., "Silver IV")</summary>
    string Rank,
    /// <summary>Timestamp of the game</summary>
    DateTime Timestamp,
    /// <summary>True if this game resulted in a promotion (rank changed up)</summary>
    bool IsPromotion,
    /// <summary>True if this game resulted in a demotion (rank changed down)</summary>
    bool IsDemotion,
    /// <summary>True if the player won this game</summary>
    bool Win
);

