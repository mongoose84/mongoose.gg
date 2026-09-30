using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Services.Solo;

/// <summary>
/// LP mode or win-rate mode for the headline, the climb card and LP per champion
/// (features/solo-trends.spec.md FR 9). One rule for all three. Pure and static.
/// </summary>
public static class LpCoverageRule
{
    public const string LpMode = "lp";
    public const string WinRateMode = "winRate";

    // FR 9: at least 80% of the ranked matches in range, and at least 10, have a known LP change.
    public const int MinCoveragePercent = 80;
    public const int MinKnownChanges = 10;

    public static string Mode(string queueType, bool singleAccount, IReadOnlyList<SoloMatchRow> rows)
    {
        // LP only means something on one ladder: one ranked queue of one account
        if (queueType is not (SoloScope.RankedSolo or SoloScope.RankedFlex) || !singleAccount) return WinRateMode;
        if (rows.Count == 0) return WinRateMode;

        var known = rows.Count(r => r.LpChange.HasValue);
        return known >= MinKnownChanges && known * 100 >= rows.Count * MinCoveragePercent
            ? LpMode
            : WinRateMode;
    }
}
