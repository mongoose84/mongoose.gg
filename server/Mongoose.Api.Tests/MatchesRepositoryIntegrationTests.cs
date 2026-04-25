using FluentAssertions;
using MySqlConnector;
using Mongoose.Api.Infrastructure.Database;
using Mongoose.Api.Infrastructure.Database.Repositories;
using Xunit;

namespace Mongoose.Api.Tests;

public sealed class MatchesRepositoryIntegrationTests
{
    [Fact]
    public async Task GetMatchListSummaryAsync_MapsAccountFields_FromRealSqlPath()
    {
        if (!IsIntegrationDbOptInEnabled())
        {
            return;
        }

        var connectionString = GetTestConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var factory = new DirectDbConnectionFactory(connectionString);
        await EnsureSchemaAsync(factory);

        var repository = new MatchesRepository(factory);

        var testKey = Guid.NewGuid().ToString("N")[..12];
        var puuid = $"integration-puuid-{testKey}";
        var matchId = $"INTEGRATION_MATCH_{testKey}";
        var gameName = $"Player{testKey}";
        var tagLine = "NA1";
        var region = "na1";

        try
        {
            await InsertRiotAccountAsync(factory, puuid, gameName, tagLine, region);
            await InsertMatchAsync(factory, matchId);
            await InsertParticipantAsync(factory, matchId, puuid);

            var result = await repository.GetMatchListSummaryAsync([puuid], string.Empty, 20, null);

            result.Should().ContainSingle();
            var item = result[0];
            item.MatchId.Should().Be(matchId);
            item.AccountGameName.Should().Be(gameName);
            item.AccountTagLine.Should().Be(tagLine);
            item.AccountRegion.Should().Be(region);
            item.QueueId.Should().Be(420);
            item.ChampionId.Should().Be(266);
        }
        finally
        {
            await CleanupAsync(factory, matchId, puuid);
        }
    }

    [Fact]
    public async Task GetMatchListSummaryAsync_ExcludesShortGames_BelowMinDuration()
    {
        if (!IsIntegrationDbOptInEnabled()) return;
        var connectionString = GetTestConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var factory = new DirectDbConnectionFactory(connectionString);
        await EnsureSchemaAsync(factory);
        var repository = new MatchesRepository(factory);

        var testKey = Guid.NewGuid().ToString("N")[..12];
        var puuid = $"integration-puuid-{testKey}";
        var shortMatchId = $"SHORT_MATCH_{testKey}";

        try
        {
            await InsertRiotAccountAsync(factory, puuid, $"Player{testKey}", "NA1", "na1");
            await InsertMatchWithDurationAsync(factory, shortMatchId, gameDurationSec: 180);
            await InsertParticipantAsync(factory, shortMatchId, puuid);

            var result = await repository.GetMatchListSummaryAsync([puuid], string.Empty, 20, null);

            result.Should().BeEmpty("a match with 180s duration is a remake and must be excluded");
        }
        finally
        {
            await CleanupAsync(factory, shortMatchId, puuid);
        }
    }

    [Fact]
    public async Task GetMatchListSummaryAsync_IncludesGames_AtExactMinDuration()
    {
        if (!IsIntegrationDbOptInEnabled()) return;
        var connectionString = GetTestConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var factory = new DirectDbConnectionFactory(connectionString);
        await EnsureSchemaAsync(factory);
        var repository = new MatchesRepository(factory);

        var testKey = Guid.NewGuid().ToString("N")[..12];
        var puuid = $"integration-puuid-{testKey}";
        var matchId = $"BOUNDARY_MATCH_{testKey}";

        try
        {
            await InsertRiotAccountAsync(factory, puuid, $"Player{testKey}", "NA1", "na1");
            await InsertMatchWithDurationAsync(factory, matchId, gameDurationSec: 300);
            await InsertParticipantAsync(factory, matchId, puuid);

            var result = await repository.GetMatchListSummaryAsync([puuid], string.Empty, 20, null);

            result.Should().ContainSingle("a match with exactly 300s duration is valid and must be included");
            result[0].MatchId.Should().Be(matchId);
        }
        finally
        {
            await CleanupAsync(factory, matchId, puuid);
        }
    }

    [Fact]
    public async Task GetRoleBaselinesAsync_ExcludesShortGames_BelowMinDuration()
    {
        if (!IsIntegrationDbOptInEnabled()) return;
        var connectionString = GetTestConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var factory = new DirectDbConnectionFactory(connectionString);
        await EnsureSchemaAsync(factory);
        var repository = new MatchesRepository(factory);

        var testKey = Guid.NewGuid().ToString("N")[..12];
        var puuid = $"integration-puuid-{testKey}";
        var shortMatchId = $"SHORT_BASELINE_{testKey}";

        try
        {
            await InsertMatchWithDurationAsync(factory, shortMatchId, gameDurationSec: 180);
            await InsertParticipantAsync(factory, shortMatchId, puuid);

            var baselines = await repository.GetRoleBaselinesAsync([puuid], string.Empty);

            baselines.Should().BeEmpty("a remake with 180s duration must not contribute to role baselines");
        }
        finally
        {
            await CleanupAsync(factory, shortMatchId, puuid);
        }
    }

    [Fact]
    public async Task GetRoleBaselinesAsync_IncludesGames_AtExactMinDuration()
    {
        if (!IsIntegrationDbOptInEnabled()) return;
        var connectionString = GetTestConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var factory = new DirectDbConnectionFactory(connectionString);
        await EnsureSchemaAsync(factory);
        var repository = new MatchesRepository(factory);

        var testKey = Guid.NewGuid().ToString("N")[..12];
        var puuid = $"integration-puuid-{testKey}";
        var matchId = $"BOUNDARY_BASELINE_{testKey}";

        try
        {
            await InsertMatchWithDurationAsync(factory, matchId, gameDurationSec: 300);
            await InsertParticipantWithRoleAsync(factory, matchId, puuid, role: "TOP");

            var baselines = await repository.GetRoleBaselinesAsync([puuid], string.Empty);

            baselines.Should().ContainKey("TOP", "a match with exactly 300s duration is valid for baselines");
        }
        finally
        {
            await CleanupAsync(factory, matchId, puuid);
        }
    }

    private static string? GetTestConnectionString()
    {
        return Environment.GetEnvironmentVariable("Database_test")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Database_test");
    }

    private static bool IsIntegrationDbOptInEnabled()
    {
        var value = Environment.GetEnvironmentVariable("RUN_DB_INTEGRATION_TESTS");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task EnsureSchemaAsync(IDbConnectionFactory factory)
    {
        await using var connection = await factory.CreateOpenConnectionAsync();

        var sql = @"
            CREATE TABLE IF NOT EXISTS riot_accounts (
                puuid VARCHAR(78) PRIMARY KEY,
                game_name VARCHAR(100) NOT NULL,
                tag_line VARCHAR(10) NOT NULL,
                summoner_name VARCHAR(100) NOT NULL,
                region VARCHAR(10) NOT NULL,
                summoner_id VARCHAR(100) NULL,
                sync_status ENUM('pending', 'syncing', 'completed', 'failed') DEFAULT 'pending',
                sync_progress INT NOT NULL DEFAULT 0,
                sync_total INT NOT NULL DEFAULT 0,
                profile_icon_id INT NULL,
                summoner_level INT NULL,
                solo_tier VARCHAR(20) NULL,
                solo_rank VARCHAR(10) NULL,
                solo_lp INT NULL,
                flex_tier VARCHAR(20) NULL,
                flex_rank VARCHAR(10) NULL,
                flex_lp INT NULL,
                last_sync_at TIMESTAMP NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            CREATE TABLE IF NOT EXISTS matches (
                match_id VARCHAR(50) PRIMARY KEY,
                queue_id INT NOT NULL,
                game_duration_sec INT NOT NULL,
                game_start_time BIGINT NOT NULL,
                patch_version VARCHAR(20) NOT NULL,
                season_code VARCHAR(20) NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            CREATE TABLE IF NOT EXISTS participants (
                id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
                match_id VARCHAR(50) NOT NULL,
                puuid VARCHAR(78) NOT NULL,
                team_id INT NOT NULL,
                role VARCHAR(20) NULL,
                lane VARCHAR(20) NULL,
                champion_id INT NOT NULL,
                champion_name VARCHAR(50) NOT NULL,
                win BOOLEAN NOT NULL,
                kills INT NOT NULL,
                deaths INT NOT NULL,
                assists INT NOT NULL,
                creep_score INT NOT NULL,
                gold_earned INT NOT NULL,
                time_dead_sec INT NOT NULL,
                lp_after INT NULL,
                tier_after VARCHAR(20) NULL,
                rank_after VARCHAR(10) NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                UNIQUE KEY idx_match_puuid (match_id, puuid),
                CONSTRAINT fk_participants_match FOREIGN KEY (match_id) REFERENCES matches(match_id) ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

        await using var cmd = new MySqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task InsertRiotAccountAsync(IDbConnectionFactory factory, string puuid, string gameName, string tagLine, string region)
    {
        const string sql = @"
            INSERT INTO riot_accounts (
                puuid, game_name, tag_line, summoner_name, region
            ) VALUES (
                @puuid, @gameName, @tagLine, @summonerName, @region
            );";

        await using var connection = await factory.CreateOpenConnectionAsync();
        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@puuid", puuid);
        cmd.Parameters.AddWithValue("@gameName", gameName);
        cmd.Parameters.AddWithValue("@tagLine", tagLine);
        cmd.Parameters.AddWithValue("@summonerName", gameName);
        cmd.Parameters.AddWithValue("@region", region);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task InsertMatchAsync(IDbConnectionFactory factory, string matchId)
    {
        const string sql = @"
            INSERT INTO matches (
                match_id, queue_id, game_duration_sec, game_start_time, patch_version, season_code
            ) VALUES (
                @matchId, 420, 1800, @gameStartTime, '15.1.1', NULL
            );";

        await using var connection = await factory.CreateOpenConnectionAsync();
        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@matchId", matchId);
        cmd.Parameters.AddWithValue("@gameStartTime", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task InsertParticipantAsync(IDbConnectionFactory factory, string matchId, string puuid)
    {
        const string sql = @"
            INSERT INTO participants (
                match_id, puuid, team_id, role, lane, champion_id, champion_name,
                win, kills, deaths, assists, creep_score, gold_earned, time_dead_sec,
                lp_after, tier_after, rank_after
            ) VALUES (
                @matchId, @puuid, 100, 'TOP', 'TOP', 266, 'Aatrox',
                TRUE, 7, 2, 6, 210, 14500, 50,
                NULL, NULL, NULL
            );";

        await using var connection = await factory.CreateOpenConnectionAsync();
        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@matchId", matchId);
        cmd.Parameters.AddWithValue("@puuid", puuid);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task InsertMatchWithDurationAsync(IDbConnectionFactory factory, string matchId, int gameDurationSec)
    {
        const string sql = @"
            INSERT INTO matches (
                match_id, queue_id, game_duration_sec, game_start_time, patch_version, season_code
            ) VALUES (
                @matchId, 420, @gameDurationSec, @gameStartTime, '15.1.1', NULL
            );";

        await using var connection = await factory.CreateOpenConnectionAsync();
        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@matchId", matchId);
        cmd.Parameters.AddWithValue("@gameDurationSec", gameDurationSec);
        cmd.Parameters.AddWithValue("@gameStartTime", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task InsertParticipantWithRoleAsync(IDbConnectionFactory factory, string matchId, string puuid, string role)
    {
        const string sql = @"
            INSERT INTO participants (
                match_id, puuid, team_id, role, lane, champion_id, champion_name,
                win, kills, deaths, assists, creep_score, gold_earned, time_dead_sec,
                lp_after, tier_after, rank_after
            ) VALUES (
                @matchId, @puuid, 100, @role, @role, 266, 'Aatrox',
                TRUE, 7, 2, 6, 210, 14500, 50,
                NULL, NULL, NULL
            );";

        await using var connection = await factory.CreateOpenConnectionAsync();
        await using var cmd = new MySqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@matchId", matchId);
        cmd.Parameters.AddWithValue("@puuid", puuid);
        cmd.Parameters.AddWithValue("@role", role);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task CleanupAsync(IDbConnectionFactory factory, string matchId, string puuid)    {
        await using var connection = await factory.CreateOpenConnectionAsync();

        await using (var deleteParticipants = new MySqlCommand("DELETE FROM participants WHERE match_id = @matchId AND puuid = @puuid;", connection))
        {
            deleteParticipants.Parameters.AddWithValue("@matchId", matchId);
            deleteParticipants.Parameters.AddWithValue("@puuid", puuid);
            await deleteParticipants.ExecuteNonQueryAsync();
        }

        await using (var deleteMatch = new MySqlCommand("DELETE FROM matches WHERE match_id = @matchId;", connection))
        {
            deleteMatch.Parameters.AddWithValue("@matchId", matchId);
            await deleteMatch.ExecuteNonQueryAsync();
        }

        await using (var deleteAccount = new MySqlCommand("DELETE FROM riot_accounts WHERE puuid = @puuid;", connection))
        {
            deleteAccount.Parameters.AddWithValue("@puuid", puuid);
            await deleteAccount.ExecuteNonQueryAsync();
        }
    }

    private sealed class DirectDbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public DirectDbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public MySqlConnection CreateConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        public async Task<MySqlConnection> CreateOpenConnectionAsync()
        {
            var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand("SET time_zone = '+00:00'", conn);
            await cmd.ExecuteNonQueryAsync();
            return conn;
        }
    }
}