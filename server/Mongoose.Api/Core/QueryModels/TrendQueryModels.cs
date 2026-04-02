namespace Mongoose.Api.Core.QueryModels;

/// <summary>
/// Core layer data models for trend-related queries.
/// NOTE: These models are not currently in use. Repositories still return Application DTOs directly.
/// These were created as part of a planned Clean Architecture refactoring (Phase 3) that was not completed.
/// To use these, repositories would need to be updated to return these Core types,
/// and endpoints would map them to Application DTOs.
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

