namespace Mongoose.Api.Core.Services.Solo;

/// <summary>A named region of the map with the fixed anchor its circle is drawn at (u right, v up).</summary>
public sealed record MapRegion(string Key, double U, double V);

/// <summary>
/// The fixed map regions of the death zones (features/solo-trends.spec.md FR 32-33). Positions are
/// normalised to 0-1 and mirrored for red-side deaths, so the player's base is always bottom left.
/// Named regions rather than clusters: stable between visits and easy to label. Pure and static.
/// </summary>
public static class MapRegions
{
    public const double MapSize = 14870;
    public const int RedTeamId = 200;

    public const string DragonPit = "dragonPit";
    public const string BaronPit = "baronPit";
    public const string YourBase = "yourBase";
    public const string EnemyBase = "enemyBase";
    public const string TopLaneYours = "topLaneYours";
    public const string TopLaneEnemy = "topLaneEnemy";
    public const string BotLaneYours = "botLaneYours";
    public const string BotLaneEnemy = "botLaneEnemy";
    public const string MidLaneYours = "midLaneYours";
    public const string MidLaneEnemy = "midLaneEnemy";
    public const string RiverTop = "riverTop";
    public const string RiverBot = "riverBot";
    public const string JungleYoursTop = "jungleYoursTop";
    public const string JungleYoursBot = "jungleYoursBot";
    public const string JungleEnemyTop = "jungleEnemyTop";
    public const string JungleEnemyBot = "jungleEnemyBot";

    private const double PitRadius = 0.06;
    private const double BaseEdge = 0.18;
    private const double LaneEdge = 0.12;
    private const double MidWidth = 0.06;
    private const double RiverWidth = 0.07;

    /// <summary>Every region with its anchor, in FR 33 order.</summary>
    public static readonly IReadOnlyList<MapRegion> All =
    [
        new(DragonPit, 0.66, 0.30),
        new(BaronPit, 0.34, 0.70),
        new(YourBase, 0.08, 0.08),
        new(EnemyBase, 0.92, 0.92),
        new(TopLaneYours, 0.06, 0.60),
        new(TopLaneEnemy, 0.40, 0.94),
        new(BotLaneYours, 0.60, 0.06),
        new(BotLaneEnemy, 0.94, 0.40),
        new(MidLaneYours, 0.40, 0.40),
        new(MidLaneEnemy, 0.60, 0.60),
        new(RiverTop, 0.25, 0.75),
        new(RiverBot, 0.75, 0.25),
        new(JungleYoursTop, 0.26, 0.61),
        new(JungleYoursBot, 0.61, 0.26),
        new(JungleEnemyTop, 0.39, 0.74),
        new(JungleEnemyBot, 0.74, 0.39)
    ];

    public static MapRegion Get(string key) => All.First(r => r.Key == key);

    /// <summary>FR 32: the position normalised to 0-1, mirrored for the red team.</summary>
    public static (double U, double V) Normalize(int x, int y, int teamId)
    {
        var u = Math.Clamp(x / MapSize, 0, 1);
        var v = Math.Clamp(y / MapSize, 0, 1);
        return teamId == RedTeamId ? (1 - u, 1 - v) : (u, v);
    }

    /// <summary>FR 33: the one region a (mirrored) position falls into, tested in order.</summary>
    public static string Classify(double u, double v)
    {
        var yourHalf = u + v < 1;
        var topSide = v > u;

        if (Distance(u, v, 0.66, 0.30) <= PitRadius) return DragonPit;
        if (Distance(u, v, 0.34, 0.70) <= PitRadius) return BaronPit;
        if (u < BaseEdge && v < BaseEdge) return YourBase;
        if (u > 1 - BaseEdge && v > 1 - BaseEdge) return EnemyBase;
        if (u < LaneEdge || v > 1 - LaneEdge) return yourHalf ? TopLaneYours : TopLaneEnemy;
        if (v < LaneEdge || u > 1 - LaneEdge) return yourHalf ? BotLaneYours : BotLaneEnemy;
        if (Math.Abs(u - v) < MidWidth) return yourHalf ? MidLaneYours : MidLaneEnemy;
        if (Math.Abs(u + v - 1) < RiverWidth) return topSide ? RiverTop : RiverBot;
        if (yourHalf) return topSide ? JungleYoursTop : JungleYoursBot;
        return topSide ? JungleEnemyTop : JungleEnemyBot;
    }

    /// <summary>The lane regions (both halves) of a role; empty for the jungle.</summary>
    public static IReadOnlyList<string> LaneOf(string role) => role switch
    {
        "TOP" => [TopLaneYours, TopLaneEnemy],
        "MIDDLE" => [MidLaneYours, MidLaneEnemy],
        "BOTTOM" or "UTILITY" => [BotLaneYours, BotLaneEnemy],
        _ => []
    };

    private static double Distance(double u, double v, double cu, double cv)
        => Math.Sqrt((u - cu) * (u - cu) + (v - cv) * (v - cv));
}
