using MySqlConnector;
using RiotProxy.External.Domain.Entities.V2;

namespace RiotProxy.Infrastructure.External.Database.Repositories.V2;

public class V2SeasonsRepository : RepositoryBase
{
    public V2SeasonsRepository(IDbConnectionFactory factory) : base(factory) {}

    public Task UpsertAsync(V2Season season)
    {
        const string sql = @"INSERT INTO seasons (season_code, patch_version, start_date, end_date, created_at)
            VALUES (@season_code, @patch_version, @start_date, @end_date, @created_at) AS new
            ON DUPLICATE KEY UPDATE
                patch_version = new.patch_version,
                start_date = new.start_date,
                end_date = new.end_date;";

        return ExecuteNonQueryAsync(sql,
            ("@season_code", season.SeasonCode),
            ("@patch_version", season.PatchVersion),
            ("@start_date", season.StartDate),
            ("@end_date", season.EndDate ?? (object)DBNull.Value),
            ("@created_at", season.CreatedAt == default ? DateTime.UtcNow : season.CreatedAt));
    }

    public Task<V2Season?> GetByCodeAsync(string seasonCode)
    {
        const string sql = "SELECT * FROM seasons WHERE season_code = @season_code LIMIT 1";
        return ExecuteSingleAsync(sql, Map, ("@season_code", seasonCode));
    }

    private static V2Season Map(MySqlDataReader r) => new()
    {
        SeasonCode = r.GetString("season_code"),
        PatchVersion = r.GetString("patch_version"),
        StartDate = DateOnly.FromDateTime(r.GetDateTime("start_date")),
        EndDate = r.IsDBNull("end_date") ? null : DateOnly.FromDateTime(r.GetDateTime("end_date")),
        CreatedAt = r.GetDateTime("created_at")
    };
}
