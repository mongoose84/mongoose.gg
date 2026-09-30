using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Services.Solo;

/// <summary>
/// Queue and range rules shared by every Solo endpoint (features/solo-trends.spec.md FR2–FR3).
/// </summary>
public static class SoloScope
{
    public const string RankedSolo = "ranked_solo";
    public const string RankedFlex = "ranked_flex";
    public const string AllQueues = "all";

    /// <summary>
    /// Parses the <c>queueType</c> parameter. A missing value is valid and means "pick the default"
    /// (<paramref name="queue"/> is null); an unknown value is invalid.
    /// </summary>
    public static bool TryParseQueue(string? raw, out string? queue)
    {
        queue = null;
        if (string.IsNullOrWhiteSpace(raw)) return true;

        var normalized = raw.Trim().ToLowerInvariant();
        if (normalized is RankedSolo or RankedFlex or AllQueues)
        {
            queue = normalized;
            return true;
        }

        return false;
    }

    /// <summary>Parses the <c>range</c> parameter; a missing value means Last 20.</summary>
    public static bool TryParseRange(string? raw, out SoloRange range)
    {
        range = SoloRange.Last20;
        if (string.IsNullOrWhiteSpace(raw)) return true;

        switch (raw.Trim().ToLowerInvariant())
        {
            case "last20":
                range = SoloRange.Last20;
                return true;
            case "last50":
                range = SoloRange.Last50;
                return true;
            case "season":
                range = SoloRange.Season;
                return true;
            default:
                return false;
        }
    }

    /// <summary>FR2: Solo/Duo when played this season, otherwise Flex, otherwise All queues.</summary>
    public static string DefaultQueue(SoloQueueCounts counts)
    {
        if (counts.RankedSolo > 0) return RankedSolo;
        if (counts.RankedFlex > 0) return RankedFlex;
        return AllQueues;
    }

    public static string RangeKey(SoloRange range) => range switch
    {
        SoloRange.Last50 => "last50",
        SoloRange.Season => "season",
        _ => "last20"
    };

    /// <summary>The match count a range asks for; null for Season (every match this season).</summary>
    public static int? RangeLimit(SoloRange range) => range switch
    {
        SoloRange.Last20 => 20,
        SoloRange.Last50 => 50,
        _ => null
    };
}
