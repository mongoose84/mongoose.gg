using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RiotProxy.Core.Entities;
using RiotProxy.Infrastructure.Database.Repositories;
using RiotProxy.Infrastructure.WebSocket;
using Xunit;

namespace RiotProxy.Tests;

/// <summary>
/// Unit tests for SyncProgressHub WebSocket functionality.
/// </summary>
public class SyncProgressHubTests : IDisposable
{
    private readonly SyncProgressHub _hub;
    private readonly FakeLogger<SyncProgressHub> _logger;
    private readonly FakeV2RiotAccountsRepositoryForHub _riotAccountsRepo;
    private readonly FakeScopeFactory _scopeFactory;

    public SyncProgressHubTests()
    {
        _logger = new FakeLogger<SyncProgressHub>();
        _riotAccountsRepo = new FakeV2RiotAccountsRepositoryForHub();
        _scopeFactory = new FakeScopeFactory(_riotAccountsRepo);
        _hub = new SyncProgressHub(_logger, _scopeFactory);
    }

    public void Dispose()
    {
        _scopeFactory.Dispose();
    }

    [Fact]
    public async Task BroadcastProgressAsync_NoSubscribers_DoesNotThrow()
    {
        // Act & Assert - should not throw when no subscribers
        await _hub.BroadcastProgressAsync("puuid-123", 5, 10, "MATCH_001");
    }

    [Fact]
    public async Task BroadcastCompleteAsync_NoSubscribers_DoesNotThrow()
    {
        // Act & Assert - should not throw when no subscribers
        await _hub.BroadcastCompleteAsync("puuid-123", 100);
    }

    [Fact]
    public async Task BroadcastErrorAsync_NoSubscribers_DoesNotThrow()
    {
        // Act & Assert - should not throw when no subscribers
        await _hub.BroadcastErrorAsync("puuid-123", "Rate limited");
    }

    [Fact]
    public void SyncProgressMessage_SerializesCorrectly()
    {
        // Arrange
        var message = new SyncProgressMessage
        {
            Puuid = "test-puuid-123",
            Status = "syncing",
            Progress = 45,
            Total = 100,
            MatchId = "EUW1_123456789"
        };

        // Act
        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        using var doc = JsonDocument.Parse(json);

        // Assert
        doc.RootElement.GetProperty("type").GetString().Should().Be("sync_progress");
        doc.RootElement.GetProperty("puuid").GetString().Should().Be("test-puuid-123");
        doc.RootElement.GetProperty("status").GetString().Should().Be("syncing");
        doc.RootElement.GetProperty("progress").GetInt32().Should().Be(45);
        doc.RootElement.GetProperty("total").GetInt32().Should().Be(100);
        doc.RootElement.GetProperty("matchId").GetString().Should().Be("EUW1_123456789");
    }

    [Fact]
    public void SyncCompleteMessage_SerializesCorrectly()
    {
        // Arrange
        var message = new SyncCompleteMessage
        {
            Puuid = "test-puuid-456",
            Status = "completed",
            TotalSynced = 150
        };

        // Act
        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        using var doc = JsonDocument.Parse(json);

        // Assert
        doc.RootElement.GetProperty("type").GetString().Should().Be("sync_complete");
        doc.RootElement.GetProperty("puuid").GetString().Should().Be("test-puuid-456");
        doc.RootElement.GetProperty("status").GetString().Should().Be("completed");
        doc.RootElement.GetProperty("totalSynced").GetInt32().Should().Be(150);
    }

    [Fact]
    public void SyncErrorMessage_SerializesCorrectly()
    {
        // Arrange
        var message = new SyncErrorMessage
        {
            Puuid = "test-puuid-789",
            Status = "failed",
            Error = "Rate limited, will retry in 60s"
        };

        // Act
        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        using var doc = JsonDocument.Parse(json);

        // Assert
        doc.RootElement.GetProperty("type").GetString().Should().Be("sync_error");
        doc.RootElement.GetProperty("puuid").GetString().Should().Be("test-puuid-789");
        doc.RootElement.GetProperty("status").GetString().Should().Be("failed");
        doc.RootElement.GetProperty("error").GetString().Should().Be("Rate limited, will retry in 60s");
    }

    [Fact]
    public void SubscribeMessage_DeserializesCorrectly()
    {
        // Arrange
        var json = """{"type":"subscribe","puuid":"test-puuid-abc"}""";

        // Act
        using var doc = JsonDocument.Parse(json);
        var type = doc.RootElement.GetProperty("type").GetString();
        var puuid = doc.RootElement.GetProperty("puuid").GetString();

        // Assert
        type.Should().Be("subscribe");
        puuid.Should().Be("test-puuid-abc");
    }

    [Fact]
    public void UnsubscribeMessage_DeserializesCorrectly()
    {
        // Arrange
        var json = """{"type":"unsubscribe","puuid":"test-puuid-xyz"}""";

        // Act
        using var doc = JsonDocument.Parse(json);
        var type = doc.RootElement.GetProperty("type").GetString();
        var puuid = doc.RootElement.GetProperty("puuid").GetString();

        // Assert
        type.Should().Be("unsubscribe");
        puuid.Should().Be("test-puuid-xyz");
    }

    private sealed class FakeLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}

/// <summary>
/// Fake V2RiotAccountsRepository for SyncProgressHub tests.
/// </summary>
internal sealed class FakeV2RiotAccountsRepositoryForHub : RiotAccountsRepository
{
    private readonly ConcurrentDictionary<string, RiotAccount> _accounts = new();

    public FakeV2RiotAccountsRepositoryForHub() : base(null!) { }

    public override Task<RiotAccount?> GetByPuuidAsync(string puuid)
    {
        _accounts.TryGetValue(puuid, out var account);
        return Task.FromResult(account);
    }

    public void AddAccount(RiotAccount account)
    {
        _accounts[account.Puuid] = account;
    }
}

internal sealed class FakeScopeFactory : IServiceScopeFactory, IDisposable
{
    private readonly ServiceProvider _provider;

    public FakeScopeFactory(RiotAccountsRepository repo)
    {
        var services = new ServiceCollection();
        services.AddSingleton<RiotAccountsRepository>(repo);
        _provider = services.BuildServiceProvider();
    }

    public IServiceScope CreateScope()
    {
        return new FakeScope(_provider);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    private sealed class FakeScope : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; }

        public FakeScope(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public void Dispose() { }
    }
}

