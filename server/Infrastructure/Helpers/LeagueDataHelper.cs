namespace Mongoose.Api.Infrastructure.Helpers;

/// <summary>
/// Centralized helper for League of Legends data constants and utilities.
/// Provides queue labels, champion icon URLs, and other League-specific helpers.
/// </summary>
public static class LeagueDataHelper
{
    /// <summary>
    /// Current Data Dragon CDN version for champion assets.
    /// Update this when a new League patch is released.
    /// </summary>
    public const string DataDragonVersion = "16.1.1";

    /// <summary>
    /// Base URL for Data Dragon CDN.
    /// </summary>
    private const string DataDragonCdnUrl = "https://ddragon.leagueoflegends.com/cdn";

    /// <summary>
    /// Converts a queue ID to a human-readable label.
    /// </summary>
    /// <param name="queueId">The Riot queue ID.</param>
    /// <returns>A display-friendly queue name.</returns>
    public static string GetQueueLabel(int queueId) => queueId switch
    {
        // Ranked
        420 => "Ranked Solo/Duo",
        440 => "Ranked Flex",
        // Normal
        400 => "Normal Draft",
        430 => "Normal Blind",
        // ARAM
        450 => "ARAM",
        // Rotating Game Modes
        900 => "ARURF",
        1900 => "URF",
        1020 => "One for All",
        1300 => "Nexus Blitz",
        1400 => "Ultimate Spellbook",
        1700 => "Arena",
        1710 => "Arena",
        // Co-op vs AI
        830 => "Co-op vs AI",
        840 => "Co-op vs AI",
        850 => "Co-op vs AI",
        // Clash
        700 => "Clash",
        // Tutorial
        2000 => "Tutorial",
        2010 => "Tutorial",
        2020 => "Tutorial",
        // Fallback
        _ => $"Queue {queueId}"
    };

    /// <summary>
    /// Gets the short form queue label (e.g., "Ranked Solo" instead of "Ranked Solo/Duo").
    /// Used in contexts where space is limited.
    /// </summary>
    /// <param name="queueId">The Riot queue ID.</param>
    /// <returns>A short display-friendly queue name.</returns>
    public static string GetQueueLabelShort(int queueId) => queueId switch
    {
        420 => "Ranked Solo",
        440 => "Ranked Flex",
        _ => GetQueueLabel(queueId)
    };

    /// <summary>
    /// Generates a Data Dragon CDN URL for a champion icon.
    /// </summary>
    /// <param name="championName">The champion name (e.g., "Cho'Gath", "Lee Sin").</param>
    /// <returns>The full URL to the champion icon image.</returns>
    public static string GetChampionIconUrl(string championName)
    {
        var normalized = NormalizeChampionName(championName);
        return $"{DataDragonCdnUrl}/{DataDragonVersion}/img/champion/{normalized}.png";
    }

    /// <summary>
    /// Normalizes a champion name for use in Data Dragon URLs.
    /// Removes spaces and special characters (e.g., "Cho'Gath" -> "ChoGath").
    /// </summary>
    /// <param name="championName">The champion name.</param>
    /// <returns>Normalized champion name for URL usage.</returns>
    public static string NormalizeChampionName(string championName)
    {
        if (string.IsNullOrEmpty(championName))
            return string.Empty;

        // Remove all non-alphanumeric characters (e.g., "Cho'Gath" -> "ChoGath", "Lee Sin" -> "LeeSin")
        return System.Text.RegularExpressions.Regex.Replace(championName, "[^A-Za-z0-9]", "");
    }
}

