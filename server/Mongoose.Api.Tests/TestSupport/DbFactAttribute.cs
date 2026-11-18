using Xunit;

namespace Mongoose.Api.Tests;

/// <summary>
/// A <see cref="FactAttribute"/> for tests that hit a real MySQL database. The test is reported as
/// <b>Skipped</b> (not silently passed) unless <c>RUN_DB_INTEGRATION_TESTS</c> is truthy and a
/// connection string is set in <c>Database_test</c> or <c>ConnectionStrings__Database_test</c>.
/// Read the connection string inside the test from <see cref="IntegrationDb.ConnectionString"/>.
/// </summary>
public sealed class DbFactAttribute : FactAttribute
{
    public DbFactAttribute()
    {
        if (!IntegrationDb.IsOptedIn)
        {
            Skip = "DB integration tests are opt-in: set RUN_DB_INTEGRATION_TESTS=true.";
        }
        else if (IntegrationDb.TryGetConnectionString() is null)
        {
            Skip = "RUN_DB_INTEGRATION_TESTS is set but Database_test / ConnectionStrings__Database_test is empty.";
        }
    }
}

internal static class IntegrationDb
{
    public static bool IsOptedIn =>
        Environment.GetEnvironmentVariable("RUN_DB_INTEGRATION_TESTS") is { } value
        && (value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase));

    public static string? TryGetConnectionString()
    {
        var value = Environment.GetEnvironmentVariable("Database_test")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Database_test");
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>The connection string; only valid inside a <see cref="DbFactAttribute"/> test.</summary>
    public static string ConnectionString =>
        TryGetConnectionString() ?? throw new InvalidOperationException("No integration DB connection string is set.");
}
