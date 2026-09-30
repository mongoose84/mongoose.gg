using Mongoose.Api.Core.Services.Solo;
using Mongoose.Api.Core.ValueObjects;

namespace Mongoose.Api.Core.Services;

/// <summary>
/// Works out the LP a ranked match gained or lost from the rank recorded after it and after the
/// player's previous match in the same queue.
/// </summary>
/// <remarks>
/// Sync records LP only on the newest ranked match at the time it runs, so a match gets a change
/// only when the match right before it in the same queue also has its LP. Promotions and demotions
/// are resolved on one ladder (<see cref="LpLadder"/>).
/// </remarks>
public static class LpChangeCalculator
{
    // A single ranked match never moves this far; a bigger jump means the recorded LP is stale
    // (read before Riot applied the match) or spans matches we don't have.
    public const int MaxPlausibleChange = 100;

    /// <summary>
    /// The LP change for a ranked match, or null when it can't be told: a side has no recorded
    /// rank, the jump is implausible, or its sign contradicts the result (a win that lost LP, or a
    /// loss that gained LP, means one of the readings is stale).
    /// </summary>
    public static int? Compute(RankSnapshot? before, RankSnapshot? after, bool win)
    {
        var start = LpLadder.Score(before);
        var end = LpLadder.Score(after);
        if (start is null || end is null) return null;

        var change = end.Value - start.Value;
        if (Math.Abs(change) > MaxPlausibleChange) return null;
        if (win && change <= 0) return null;
        if (!win && change > 0) return null;

        return change;
    }
}
