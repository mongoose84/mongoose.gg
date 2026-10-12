using System.Text;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Infrastructure.Database.Repositories;

public class MatchGoldFramesRepository : RepositoryBase, IMatchGoldFramesRepository
{
    public MatchGoldFramesRepository(IDbConnectionFactory factory) : base(factory) {}

    /// <inheritdoc />
    public Task ReplaceForMatchAsync(string matchId, IReadOnlyList<MatchGoldFrame> frames)
    {
        return ExecuteTransactionAsync(async (conn, tx) =>
        {
            await ExecuteNonQueryWithConnectionAsync(conn, tx,
                "DELETE FROM match_gold_frames WHERE match_id = @match_id",
                ("@match_id", matchId));

            if (frames.Count == 0) return;

            var sb = new StringBuilder("INSERT INTO match_gold_frames (match_id, minute, team100_gold, team200_gold) VALUES ");
            var parameters = new List<(string name, object? value)> { ("@match_id", matchId) };

            for (var i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                sb.Append(i == 0 ? "" : ",");
                sb.Append($"(@match_id, @f{i}_minute, @f{i}_team100_gold, @f{i}_team200_gold)");

                parameters.Add(($"@f{i}_minute", frame.Minute));
                parameters.Add(($"@f{i}_team100_gold", frame.Team100Gold));
                parameters.Add(($"@f{i}_team200_gold", frame.Team200Gold));
            }

            await ExecuteNonQueryWithConnectionAsync(conn, tx, sb.ToString(), parameters.ToArray());
        });
    }
}
