namespace Mongoose.Api.Core.Entities;

/// <summary>
/// Each team's total gold at one minute of a match (match_gold_frames, from the timeline).
/// </summary>
public class MatchGoldFrame : EntityBase
{
    public string MatchId { get; set; } = string.Empty;
    public int Minute { get; set; }
    public int Team100Gold { get; set; }
    public int Team200Gold { get; set; }
}
