namespace Mongoose.Api.Infrastructure.Database.Repositories;

/// <summary>
/// SQL shared by the repositories that work out a ranked match's LP change (Matches list, match
/// details, Solo climb), so every page reads LP the same way.
/// </summary>
internal static class PreviousRankSql
{
    // Ranked Solo/Duo and Ranked Flex: the queues that record LP
    public const string RankedQueueIds = "420, 440";

    /// <summary>
    /// Each ranked participant row with the rank recorded after the same player's previous match in
    /// the same queue. LAG takes the match right before, never the last one that happens to have LP,
    /// so a gap in the recorded LP gives no change instead of a wrong one. The predicate filters
    /// <c>p2.puuid</c> with parameters the caller supplies. Join it on <c>participant_id</c> and read
    /// <c>prev_lp_after</c>, <c>prev_tier_after</c> and <c>prev_rank_after</c>.
    /// </summary>
    public static string For(string puuidPredicate) => $@"
                SELECT
                    p2.id AS participant_id,
                    LAG(p2.lp_after) OVER previous_match AS prev_lp_after,
                    LAG(p2.tier_after) OVER previous_match AS prev_tier_after,
                    LAG(p2.rank_after) OVER previous_match AS prev_rank_after
                FROM participants p2
                INNER JOIN matches m2 ON m2.match_id = p2.match_id
                WHERE {puuidPredicate}
                AND m2.queue_id IN ({RankedQueueIds})
                WINDOW previous_match AS (PARTITION BY p2.puuid, m2.queue_id ORDER BY m2.game_start_time, m2.match_id)";
}
