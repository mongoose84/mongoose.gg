using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Services;

/// <summary>
/// Computes trend badges by comparing a match's stats against the player's role baseline.
/// Trend badges highlight the most notable performance insight for a match.
/// </summary>
public static class TrendBadgeCalculator
{
    // Require a small sample before surfacing "vs trend" insights to avoid noisy baselines.
    private const int MinimumBaselineGames = 3;

    // Surface damage insights only when the match is at least 20% above/below the player's norm.
    private const double DamageDeviationThreshold = 0.20;

    // Reserve "tankier than usual" for clearly higher incoming damage than baseline.
    private const double TankinessDeviationThreshold = 0.25;

    // Use a larger threshold for deaths so normal match variance does not trigger a badge.
    private const double DeathDeviationThreshold = 0.30;

    // Only call out a clean game when deaths stayed notably low in absolute terms too.
    private const int CleanGameMaxDeaths = 3;

    // Ignore vision comparisons for roles/builds where vision score is too low to be meaningful.
    private const double MinimumVisionBaseline = 10;
    private const double VisionDeviationThreshold = 0.25;

    // Ignore CS efficiency callouts unless the role baseline is lane-farm relevant and clearly above trend.
    private const double MinimumCsPerMinuteBaseline = 4;
    private const double CsDeviationThreshold = 0.15;

    // Require a clear outlier before highlighting teamfight involvement or KDA spikes.
    private const double KillParticipationDeviationThreshold = 0.20;
    private const double KdaDeviationThreshold = 0.25;

    /// <summary>
    /// Computes the most notable trend badge for a full match list item compared to role baseline.
    /// </summary>
    public static TrendBadge? ComputeTrendBadge(MatchListRawData match, RoleBaseline baseline)
    {
        if (baseline.GamesCount < MinimumBaselineGames) return null; // Not enough data for meaningful comparison

        var durationMin = match.GameDurationSec / 60.0;
        var csPerMin = durationMin > 0 ? match.CreepScore / durationMin : 0;

        // Calculate deviations from baseline (as percentage difference)
        var insights = new List<(string text, string type, string stat, double deviation)>();

        // Damage dealt comparison
        if (baseline.AvgDamageDealt > 0)
        {
            var damageDeviation = (match.DamageDealt - baseline.AvgDamageDealt) / baseline.AvgDamageDealt;
            if (damageDeviation > DamageDeviationThreshold)
                insights.Add(("Above avg damage", "positive", "damageDealt", damageDeviation));
            else if (damageDeviation < -DamageDeviationThreshold)
                insights.Add(("Below avg damage", "neutral", "damageDealt", Math.Abs(damageDeviation)));
        }

        // Damage taken comparison (higher can be good for tanks)
        if (baseline.AvgDamageTaken > 0)
        {
            var takenDeviation = (match.DamageTaken - baseline.AvgDamageTaken) / baseline.AvgDamageTaken;
            if (takenDeviation > TankinessDeviationThreshold)
                insights.Add(("Tankier than usual", "positive", "damageTaken", takenDeviation));
        }

        // Deaths comparison (lower is better)
        if (baseline.AvgDeaths > 0)
        {
            var deathDeviation = (match.Deaths - baseline.AvgDeaths) / baseline.AvgDeaths;
            if (deathDeviation > DeathDeviationThreshold)
                insights.Add(("Higher deaths vs trend", "neutral", "deaths", deathDeviation));
            else if (deathDeviation < -DeathDeviationThreshold && match.Deaths <= CleanGameMaxDeaths)
                insights.Add(("Clean game", "positive", "deaths", Math.Abs(deathDeviation)));
        }

        // Vision score comparison (for support/jungle)
        if (baseline.AvgVisionScore > MinimumVisionBaseline && match.VisionScore > 0)
        {
            var visionDeviation = (match.VisionScore - baseline.AvgVisionScore) / baseline.AvgVisionScore;
            if (visionDeviation > VisionDeviationThreshold)
                insights.Add(("Strong vision control", "positive", "visionScore", visionDeviation));
        }

        // CS comparison (for laners)
        if (baseline.AvgCsPerMin > MinimumCsPerMinuteBaseline && csPerMin > 0)
        {
            var csDeviation = (csPerMin - baseline.AvgCsPerMin) / baseline.AvgCsPerMin;
            if (csDeviation > CsDeviationThreshold)
                insights.Add(("High CS efficiency", "positive", "csPerMin", csDeviation));
        }

        // Kill participation
        if (baseline.AvgKillParticipation > 0)
        {
            var kpDeviation = ((double)match.KillParticipation - baseline.AvgKillParticipation) / baseline.AvgKillParticipation;
            if (kpDeviation > KillParticipationDeviationThreshold)
                insights.Add(("High kill participation", "positive", "killParticipation", kpDeviation));
        }

        // Return the most significant insight (highest deviation)
        if (insights.Count == 0) return null;

        var best = insights.OrderByDescending(i => i.deviation).First();
        return new TrendBadge(best.text, best.type, best.stat);
    }

    /// <summary>
    /// Computes trend badge for summary list (uses limited data available).
    /// </summary>
    public static TrendBadge? ComputeTrendBadgeSummary(MatchListSummaryRawData match, RoleBaseline baseline)
    {
        if (baseline.GamesCount < MinimumBaselineGames) return null;

        var durationMin = match.GameDurationSec / 60.0;
        var csPerMin = durationMin > 0 ? match.CreepScore / durationMin : 0;

        var insights = new List<(string text, string type, string stat, double deviation)>();

        // Deaths comparison (lower is better)
        if (baseline.AvgDeaths > 0)
        {
            var deathDeviation = (match.Deaths - baseline.AvgDeaths) / baseline.AvgDeaths;
            if (deathDeviation > DeathDeviationThreshold)
                insights.Add(("Higher deaths vs trend", "neutral", "deaths", deathDeviation));
            else if (deathDeviation < -DeathDeviationThreshold && match.Deaths <= CleanGameMaxDeaths)
                insights.Add(("Clean game", "positive", "deaths", Math.Abs(deathDeviation)));
        }

        // CS comparison (for laners)
        if (baseline.AvgCsPerMin > MinimumCsPerMinuteBaseline && csPerMin > 0)
        {
            var csDeviation = (csPerMin - baseline.AvgCsPerMin) / baseline.AvgCsPerMin;
            if (csDeviation > CsDeviationThreshold)
                insights.Add(("High CS efficiency", "positive", "csPerMin", csDeviation));
        }

        // KDA comparison
        var kda = match.Deaths == 0 ? match.Kills + match.Assists : (match.Kills + match.Assists) / (double)match.Deaths;
        if (baseline.AvgKda > 0)
        {
            var kdaDeviation = (kda - baseline.AvgKda) / baseline.AvgKda;
            if (kdaDeviation > KdaDeviationThreshold)
                insights.Add(("Strong KDA", "positive", "kda", kdaDeviation));
        }

        if (insights.Count == 0) return null;

        var best = insights.OrderByDescending(i => i.deviation).First();
        return new TrendBadge(best.text, best.type, best.stat);
    }
}
