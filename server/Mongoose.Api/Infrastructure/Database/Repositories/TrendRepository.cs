using MySqlConnector;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

/// <summary>
/// Match activity for the heatmap: matches played per day.
/// </summary>
public class TrendRepository : RepositoryBase, ITrendRepository
{
    public TrendRepository(IDbConnectionFactory factory) : base(factory)
    {
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, int>> GetDailyMatchCountsAsync(string puuid, int daysBack = 91)
        => await GetDailyMatchCountsAsync([puuid], daysBack);

    /// <inheritdoc />
    public async Task<Dictionary<string, int>> GetDailyMatchCountsAsync(IReadOnlyList<string> puuids, int daysBack = 91)
    {
        if (puuids.Count == 0)
            return new Dictionary<string, int>();

        var (puuidPredicate, puuidParams) = BuildStringInClause("p.puuid", puuids, "puuid");
        var startDate = DateTime.UtcNow.Date.AddDays(-daysBack);
        var startTimestamp = new DateTimeOffset(startDate).ToUnixTimeMilliseconds();

        var sql = $@"
            SELECT
                DATE(FROM_UNIXTIME(m.game_start_time / 1000)) as game_date,
                COUNT(DISTINCT m.match_id) as match_count
            FROM participants p
            INNER JOIN matches m ON m.match_id = p.match_id
            WHERE {puuidPredicate}
              AND m.game_start_time >= @start_timestamp
            GROUP BY DATE(FROM_UNIXTIME(m.game_start_time / 1000))
            ORDER BY game_date ASC";

        var result = new Dictionary<string, int>();

        await ExecuteWithConnectionAsync(async conn =>
        {
            await using var cmd = new MySqlCommand(sql, conn);
            foreach (var (name, value) in puuidParams)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            cmd.Parameters.AddWithValue("@start_timestamp", startTimestamp);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var gameDate = reader.GetDateTimeUtc(0);
                var matchCount = reader.GetInt32(1);
                result[gameDate.ToString("yyyy-MM-dd")] = matchCount;
            }
            return 0;
        });

        return result;
    }
}
