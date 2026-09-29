using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Services.Solo;

internal static class SoloMath
{
    /// <summary>Whole-number win rate (FR30); 0 for an empty list.</summary>
    public static int WinRate(IReadOnlyCollection<SoloMatchRow> rows)
        => rows.Count == 0 ? 0 : Percent(rows.Count(r => r.Win), rows.Count);

    public static int Percent(int part, int total)
        => total == 0 ? 0 : (int)Math.Round(100.0 * part / total, MidpointRounding.AwayFromZero);
}
