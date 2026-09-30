using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Services.Solo;

public enum StatDirection
{
    HigherIsBetter,
    LowerIsBetter
}

/// <summary>
/// One of the six match-deciding stats (features/solo-trends.spec.md FR13): how to read it from a match,
/// which way is better, how big a change counts, and its win factor (FR21) when it has one.
/// </summary>
public sealed record SoloStatDefinition(
    string Key,
    StatDirection Direction,
    double SteadyThreshold,
    Func<SoloMatchRow, double?> Value,
    string? FactorKey);

public static class SoloStats
{
    public const string Deaths = "deaths";
    public const string GoldLeadAt15 = "goldLeadAt15";
    public const string DragonParticipation = "dragonParticipation";
    public const string VisionPerMin = "visionPerMin";
    public const string CsPerMin = "csPerMin";
    public const string KillParticipation = "killParticipation";

    internal const string Utility = "UTILITY";
    internal const string Jungle = "JUNGLE";

    // FR13: gold at 15 needs a match that reached 15 minutes.
    internal const int FifteenMinutesSec = 900;

    // FR13: kill participation is meaningless when the team barely got kills.
    private const int MinTeamKillsForKillParticipation = 5;

    /// <summary>The six stats in FR13 order (strongest win predictors first); also every tie-break order.</summary>
    public static readonly IReadOnlyList<SoloStatDefinition> All =
    [
        new(Deaths, StatDirection.LowerIsBetter, 0.5, r => r.Deaths, WinFactorCalculator.LowDeaths),
        new(GoldLeadAt15, StatDirection.HigherIsBetter, 150, GoldLeadAt15Value, WinFactorCalculator.AheadAt15),
        new(DragonParticipation, StatDirection.HigherIsBetter, 5, DragonParticipationValue, WinFactorCalculator.Dragons),
        new(VisionPerMin, StatDirection.HigherIsBetter, 0.1, r => r.VisionPerMin, WinFactorCalculator.Vision),
        new(CsPerMin, StatDirection.HigherIsBetter, 0.3, CsPerMinValue, WinFactorCalculator.Cs),
        new(KillParticipation, StatDirection.HigherIsBetter, 3, KillParticipationValue, null)
    ];

    public static SoloStatDefinition Get(string key) => All.First(s => s.Key == key);

    internal static double? GoldLeadAt15Value(SoloMatchRow r)
        => r.DurationSec >= FifteenMinutesSec ? r.GoldDiffAt15 : null;

    internal static double? DragonParticipationValue(SoloMatchRow r)
        => r.TeamDragons is > 0 && r.DragonsParticipated.HasValue
            ? 100.0 * r.DragonsParticipated.Value / r.TeamDragons.Value
            : null;

    internal static double? CsPerMinValue(SoloMatchRow r)
        => r.Role == Utility || r.DurationSec <= 0 ? null : r.CreepScore / (r.DurationSec / 60.0);

    internal static double? KillParticipationValue(SoloMatchRow r)
        => r.TeamKills >= MinTeamKillsForKillParticipation ? r.KillParticipationPct : null;
}
