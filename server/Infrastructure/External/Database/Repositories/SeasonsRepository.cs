using MySqlConnector;
using RiotProxy.External.Domain.Entities;

namespace RiotProxy.Infrastructure.External.Database.Repositories;

public class SeasonsRepository : RepositoryBase
{
    public SeasonsRepository(IDbConnectionFactory factory) : base(factory) {}

    public Task UpsertAsync(Season season)
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

    public Task<Season?> GetByCodeAsync(string seasonCode)
    {
        const string sql = "SELECT * FROM seasons WHERE season_code = @season_code LIMIT 1";
        return ExecuteSingleAsync(sql, Map, ("@season_code", seasonCode));
    }

    private static Season Map(MySqlDataReader r) => new()
    {
        SeasonCode = r.GetString(0),
        PatchVersion = r.GetString(1),
        StartDate = DateOnly.FromDateTime(r.GetDateTime(2)),
        EndDate = r.IsDBNull(3) ? null : DateOnly.FromDateTime(r.GetDateTime(3)),
        CreatedAt = r.GetDateTimeUtc(4)
    };
}
