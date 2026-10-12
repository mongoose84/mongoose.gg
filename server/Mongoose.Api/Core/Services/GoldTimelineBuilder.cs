using Mongoose.Api.Core.QueryModels;

namespace Mongoose.Api.Core.Services;

/// <summary>
/// Builds the match-details gold-lead chart (design-migration.plan.md, 4d): the lead from the
/// player's side, the objectives they and the enemy took, and the biggest swing. Pure and static.
/// </summary>
public static class GoldTimelineBuilder
{
    // Fewer points than this (two 5-minute checkpoints) don't draw a line worth reading.
    private const int MinPoints = 3;

    // A swing is the change in lead over at most this many minutes: long enough to catch a
    // fight and the objective after it, short enough that a slow snowball isn't one.
    private const int SwingWindowMinutes = 3;

    // Smaller changes are ordinary trading, not a moment worth an orange point.
    public const int MinSwing = 1500;

    // The design system's moment letters: D, B, H, T. Grubs and inhibitors stay off the axis.
    private static readonly HashSet<string> MomentTypes = ["dragon", "baron", "herald", "tower"];

    public static GoldTimeline? Build(GoldTimelineData data)
    {
        if (data.Frames.Count < MinPoints || data.TeamId is not (100 or 200)) return null;

        var points = data.Frames
            .OrderBy(f => f.Minute)
            .Select(f => new GoldTimelinePoint(
                f.Minute,
                data.TeamId == 100 ? f.Team100Gold - f.Team200Gold : f.Team200Gold - f.Team100Gold))
            .ToList();

        var moments = data.Objectives
            .Where(o => MomentTypes.Contains(o.Type))
            .OrderBy(o => o.TimestampSec)
            .Select(o => new GoldTimelineMoment(o.TimestampSec, o.Type, o.TeamId == data.TeamId))
            .ToList();

        return new GoldTimeline(
            Source: data.FromCheckpoints ? "checkpoints" : "minute",
            Points: points,
            Moments: moments,
            Swing: BiggestSwing(points));
    }

    /// <summary>
    /// The largest change in lead between a point and an earlier one at most
    /// <see cref="SwingWindowMinutes"/> before it (always including the point right before, so
    /// 5-minute checkpoints still compare neighbours). The earliest wins a tie; null under
    /// <see cref="MinSwing"/>.
    /// </summary>
    private static GoldTimelineSwing? BiggestSwing(IReadOnlyList<GoldTimelinePoint> points)
    {
        GoldTimelineSwing? best = null;

        for (var i = 1; i < points.Count; i++)
        {
            for (var j = i - 1; j >= 0; j--)
            {
                if (j < i - 1 && points[i].Minute - points[j].Minute > SwingWindowMinutes) break;

                var change = points[i].Lead - points[j].Lead;
                if (Math.Abs(change) > Math.Abs(best?.Change ?? 0))
                    best = new GoldTimelineSwing(points[j].Minute, points[i].Minute, change);
            }
        }

        return best is { } swing && Math.Abs(swing.Change) >= MinSwing ? swing : null;
    }
}
