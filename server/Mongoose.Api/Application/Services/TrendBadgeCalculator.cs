using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Application.Services;

/// <summary>
/// Computes trend badges by comparing a match's stats against the player's role baseline.
/// Trend badges highlight the most notable performance insight for a match.
/// </summary>
public static class TrendBadgeCalculator
{
    /// <summary>
    /// Computes the most notable trend badge for a full match list item compared to role baseline.
    /// </summary>
    public static TrendBadge? ComputeTrendBadge(MatchListRawData match, RoleBaseline baseline)
    {
        if (baseline.GamesCount < 3) return null; // Not enough data for meaningful comparison

        var durationMin = match.GameDurationSec / 60.0;
        var csPerMin = durationMin > 0 ? match.CreepScore / durationMin : 0;

        // Calculate deviations from baseline (as percentage difference)
        var insights = new List<(string text, string type, string stat, double deviation)>();

        // Damage dealt comparison
        if (baseline.AvgDamageDealt > 0)
        {
            var damageDeviation = (match.DamageDealt - baseline.AvgDamageDealt) / baseline.AvgDamageDealt;
            if (damageDeviation > 0.2)
                insights.Add(("Above avg damage", "positive", "damageDealt", damageDeviation));
            else if (damageDeviation < -0.2)
                insights.Add(("Below avg damage", "neutral", "damageDealt", Math.Abs(damageDeviation)));
        }

        // Damage taken comparison (higher can be good for tanks)
        if (baseline.AvgDamageTaken > 0)
        {
            var takenDeviation = (match.DamageTaken - baseline.AvgDamageTaken) / baseline.AvgDamageTaken;
            if (takenDeviation > 0.25)
                insights.Add(("Tankier than usual", "positive", "damageTaken", takenDeviation));
        }

        // Deaths comparison (lower is better)
        if (baseline.AvgDeaths > 0)
        {
            var deathDeviation = (match.Deaths - baseline.AvgDeaths) / baseline.AvgDeaths;
            if (deathDeviation > 0.3)
                insights.Add(("Higher deaths vs trend", "neutral", "deaths", deathDeviation));
            else if (deathDeviation < -0.3 && match.Deaths <= 3)
                insights.Add(("Clean game", "positive", "deaths", Math.Abs(deathDeviation)));
        }

        // Vision score comparison (for support/jungle)
        if (baseline.AvgVisionScore > 10 && match.VisionScore > 0)
        {
            var visionDeviation = (match.VisionScore - baseline.AvgVisionScore) / baseline.AvgVisionScore;
            if (visionDeviation > 0.25)
                insights.Add(("Strong vision control", "positive", "visionScore", visionDeviation));
        }

        // CS comparison (for laners)
        if (baseline.AvgCsPerMin > 4 && csPerMin > 0)
        {
            var csDeviation = (csPerMin - baseline.AvgCsPerMin) / baseline.AvgCsPerMin;
            if (csDeviation > 0.15)
                insights.Add(("High CS efficiency", "positive", "csPerMin", csDeviation));
        }

        // Kill participation
        if (baseline.AvgKillParticipation > 0)
        {
            var kpDeviation = ((double)match.KillParticipation - baseline.AvgKillParticipation) / baseline.AvgKillParticipation;
            if (kpDeviation > 0.2)
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
        if (baseline.GamesCount < 3) return null;

        var durationMin = match.GameDurationSec / 60.0;
        var csPerMin = durationMin > 0 ? match.CreepScore / durationMin : 0;

        var insights = new List<(string text, string type, string stat, double deviation)>();

        // Deaths comparison (lower is better)
        if (baseline.AvgDeaths > 0)
        {
            var deathDeviation = (match.Deaths - baseline.AvgDeaths) / baseline.AvgDeaths;
            if (deathDeviation > 0.3)
                insights.Add(("Higher deaths vs trend", "neutral", "deaths", deathDeviation));
            else if (deathDeviation < -0.3 && match.Deaths <= 3)
                insights.Add(("Clean game", "positive", "deaths", Math.Abs(deathDeviation)));
        }

        // CS comparison (for laners)
        if (baseline.AvgCsPerMin > 4 && csPerMin > 0)
        {
            var csDeviation = (csPerMin - baseline.AvgCsPerMin) / baseline.AvgCsPerMin;
            if (csDeviation > 0.15)
                insights.Add(("High CS efficiency", "positive", "csPerMin", csDeviation));
        }

        // KDA comparison
        var kda = match.Deaths == 0 ? match.Kills + match.Assists : (match.Kills + match.Assists) / (double)match.Deaths;
        if (baseline.AvgKda > 0)
        {
            var kdaDeviation = (kda - baseline.AvgKda) / baseline.AvgKda;
            if (kdaDeviation > 0.25)
                insights.Add(("Strong KDA", "positive", "kda", kdaDeviation));
        }

        if (insights.Count == 0) return null;

        var best = insights.OrderByDescending(i => i.deviation).First();
        return new TrendBadge(best.text, best.type, best.stat);
    }
}
