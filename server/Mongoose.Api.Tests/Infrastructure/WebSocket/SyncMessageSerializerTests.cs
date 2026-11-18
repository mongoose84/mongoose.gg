using System.Text;
using System.Text.Json;
using FluentAssertions;
using Mongoose.Api.Infrastructure.WebSocket;
using Xunit;

namespace Mongoose.Api.Tests;

public class SyncMessageSerializerTests
{
    private static JsonElement Parse(object message)
        => JsonDocument.Parse(Encoding.UTF8.GetString(SyncMessageSerializer.Serialize(message))).RootElement;

    [Fact]
    public void Serialize_KeepsTheDerivedFields_OfAMessagePassedAsItsBaseType()
    {
        SyncAggregateMessage message = new SyncAggregateProgressMessage
        {
            Status = "running", Progress = 12, Total = 40, AccountsTotal = 2, AccountsDone = 1
        };

        var json = Parse(message);

        json.GetProperty("type").GetString().Should().Be("sync_aggregate_progress");
        json.GetProperty("progress").GetInt32().Should().Be(12);
        json.GetProperty("total").GetInt32().Should().Be(40);
        json.GetProperty("accountsDone").GetInt32().Should().Be(1);
    }

    [Fact]
    public void Serialize_WritesTheBackfillProgress()
    {
        SyncAggregateMessage message = new DetailBackfillProgressMessage
        {
            Status = "waiting", Done = 12, Total = 50, RetryAt = new DateTime(2026, 10, 1, 18, 4, 0, DateTimeKind.Utc)
        };

        var json = Parse(message);

        json.GetProperty("type").GetString().Should().Be("detail_backfill_progress");
        json.GetProperty("status").GetString().Should().Be("waiting");
        json.GetProperty("done").GetInt32().Should().Be(12);
        json.GetProperty("total").GetInt32().Should().Be(50);
        json.GetProperty("retryAt").GetString().Should().StartWith("2026-10-01T18:04:00");
    }
}
