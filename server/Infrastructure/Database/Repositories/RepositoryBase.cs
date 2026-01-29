using MySqlConnector;

namespace RiotProxy.Infrastructure.Database.Repositories;

/// <summary>
/// Extension methods for MySqlDataReader to ensure DateTime values are read with UTC kind.
/// </summary>
public static class MySqlDataReaderExtensions
{
    /// <summary>
    /// Gets a DateTime value from the reader and specifies it as UTC.
    /// MySQL TIMESTAMP columns store UTC values, so we explicitly mark them as such.
    /// </summary>
    public static DateTime GetDateTimeUtc(this MySqlDataReader reader, int ordinal)
    {
        var dateTime = reader.GetDateTime(ordinal);
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }

    /// <summary>
    /// Gets a nullable DateTime value from the reader and specifies it as UTC if not null.
    /// </summary>
    public static DateTime? GetDateTimeUtcOrNull(this MySqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return null;
        var dateTime = reader.GetDateTime(ordinal);
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }
}

/// <summary>
/// Base class for repositories providing common database operation helpers.
/// Reduces connection boilerplate and provides consistent patterns for queries.
/// </summary>
public abstract class RepositoryBase
{
    protected readonly IDbConnectionFactory _factory;

    protected RepositoryBase(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Execute a query that returns a single scalar value.
    /// </summary>
    protected async Task<T?> ExecuteScalarAsync<T>(string sql, params (string name, object? value)[] parameters)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        
        var result = await cmd.ExecuteScalarAsync();
        
        if (result == null || result == DBNull.Value)
            return default;
        
        return (T)Convert.ChangeType(result, typeof(T));
    }

    /// <summary>
    /// Execute a query that returns a single row, mapped by the provided function.
    /// </summary>
    protected async Task<T?> ExecuteSingleAsync<T>(string sql, Func<MySqlDataReader, T> mapper, params (string name, object? value)[] parameters) where T : class
    {
        await using var conn = await _factory.CreateOpenConnectionAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        
        await using var reader = await cmd.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            return mapper(reader);
        }
        
        return null;
    }

    /// <summary>
    /// Execute a query that returns multiple rows, mapped by the provided function.
    /// </summary>
    protected async Task<IList<T>> ExecuteListAsync<T>(string sql, Func<MySqlDataReader, T> mapper, params (string name, object? value)[] parameters)
    {
        var results = new List<T>();
        await using var conn = await _factory.CreateOpenConnectionAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        
        await using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            results.Add(mapper(reader));
        }
        
        return results;
    }

    /// <summary>
    /// Execute a non-query command (INSERT, UPDATE, DELETE).
    /// Returns the number of affected rows.
    /// </summary>
    protected async Task<int> ExecuteNonQueryAsync(string sql, params (string name, object? value)[] parameters)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        
        return await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Execute a command with full control over the connection and command.
    /// Use this for complex queries that don't fit the other patterns.
    /// </summary>
    protected async Task<T> ExecuteWithConnectionAsync<T>(Func<MySqlConnection, Task<T>> action)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync();
        return await action(conn);
    }

    /// <summary>
    /// Execute a command with full control over the connection and a pre-created command.
    /// Use this for complex queries with dynamic SQL that don't fit the other patterns.
    /// </summary>
    protected async Task<T> ExecuteWithConnectionAsync<T>(Func<MySqlConnection, MySqlCommand, Task<T>> action)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync();
        await using var cmd = new MySqlCommand { Connection = conn };
        return await action(conn, cmd);
    }

    /// <summary>
    /// Execute multiple commands within a transaction.
    /// </summary>
    protected async Task ExecuteTransactionAsync(Func<MySqlConnection, MySqlTransaction, Task> action)
    {
        await using var conn = await _factory.CreateOpenConnectionAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            await action(conn, transaction);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Execute a non-query command using an existing connection and transaction.
    /// </summary>
    protected static async Task<int> ExecuteNonQueryWithConnectionAsync(MySqlConnection conn, MySqlTransaction transaction, string sql, params (string name, object? value)[] parameters)
    {
        await using var cmd = new MySqlCommand(sql, conn, transaction);

        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return await cmd.ExecuteNonQueryAsync();
    }
}
