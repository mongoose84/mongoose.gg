using RiotProxy.Core.Entities;
using RiotProxy.Infrastructure.Database.Repositories;

namespace RiotProxy.Infrastructure.Riot;

/// <summary>
/// Helper for calculating and managing League of Legends seasons.
/// Seasons are derived from patch versions (e.g., patch "15.3" = Season 15 = "S15").
/// </summary>
public static class SeasonHelper
{
    /// <summary>
    /// Calculates the season code from a patch version.
    /// E.g., "15.3" -> "S15", "16.1" -> "S16"
    /// </summary>
    public static string? GetSeasonCodeFromPatch(string? patchVersion)
    {
        if (string.IsNullOrEmpty(patchVersion))
            return null;

        var parts = patchVersion.Split('.');
        if (parts.Length < 1 || !int.TryParse(parts[0], out var majorVersion))
            return null;

        return $"S{majorVersion}";
    }

    /// <summary>
    /// Calculates the start date for a season based on historical patterns.
    /// LoL seasons typically start in early January.
    /// </summary>
    public static DateOnly GetSeasonStartDate(int majorVersion)
    {
        // Season start dates follow a pattern:
        // S14 started ~Jan 2024, S15 ~Jan 2025, S16 ~Jan 2026
        // Base year for S14 is 2024
        var baseYear = 2024;
        var baseSeason = 14;
        
        var yearOffset = majorVersion - baseSeason;
        var year = baseYear + yearOffset;
        
        // Seasons typically start around January 8-10
        return new DateOnly(year, 1, 8);
    }

    /// <summary>
    /// Ensures the season exists in the database. Creates it if it doesn't exist.
    /// </summary>
    public static async Task<string?> EnsureSeasonExistsAsync(
        SeasonsRepository seasonsRepo,
        string? patchVersion,
        long gameStartTimestamp)
    {
        var seasonCode = GetSeasonCodeFromPatch(patchVersion);
        if (seasonCode == null)
            return null;

        // Check if season already exists
        var existingSeason = await seasonsRepo.GetByCodeAsync(seasonCode);
        if (existingSeason != null)
            return seasonCode;

        // Parse major version for start date calculation
        var parts = patchVersion!.Split('.');
        if (!int.TryParse(parts[0], out var majorVersion))
            return null;

        // Create new season
        var season = new Season
        {
            SeasonCode = seasonCode,
            PatchVersion = patchVersion,
            StartDate = GetSeasonStartDate(majorVersion),
            EndDate = null, // Current season has no end date
            CreatedAt = DateTime.UtcNow
        };

        await seasonsRepo.UpsertAsync(season);
        return seasonCode;
    }
}

