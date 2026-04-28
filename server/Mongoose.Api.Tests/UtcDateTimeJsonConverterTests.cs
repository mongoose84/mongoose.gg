using System.Text.Json;
using FluentAssertions;
using Mongoose.Api.Infrastructure.Serialization;
using Xunit;

namespace Mongoose.Api.Tests;

public class UtcDateTimeJsonConverterTests
{
    private static JsonSerializerOptions OptionsWithConverter()
    {
        var opts = new JsonSerializerOptions();
        opts.Converters.Add(new UtcDateTimeJsonConverter());
        return opts;
    }

    private static JsonSerializerOptions NullableOptionsWithConverter()
    {
        var opts = new JsonSerializerOptions();
        opts.Converters.Add(new UtcNullableDateTimeJsonConverter());
        return opts;
    }

    // ─────────────── Write (serialize) ───────────────

    [Fact]
    public void Write_SerializesUtcDateTime_WithZSuffix()
    {
        var value = new DateTime(2024, 6, 15, 12, 30, 45, 123, DateTimeKind.Utc);
        var opts = OptionsWithConverter();

        var json = JsonSerializer.Serialize(value, opts);

        json.Should().Be("\"2024-06-15T12:30:45.123Z\"");
    }

    [Fact]
    public void Write_ConvertsLocalDateTimeToUtc_BeforeSerializing()
    {
        // Use a fixed UTC offset to make the test deterministic
        var utcTime = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var localTime = utcTime.ToLocalTime(); // Local kind, same instant

        var opts = OptionsWithConverter();
        var json = JsonSerializer.Serialize(localTime, opts);

        // Deserialized back should equal the original UTC instant
        var deserialized = JsonSerializer.Deserialize<DateTime>(json, opts);
        deserialized.ToUniversalTime().Should().Be(utcTime);
    }

    [Fact]
    public void Write_TreatsUnspecifiedDateTimeAsUtc_AndAddsZSuffix()
    {
        var value = DateTime.SpecifyKind(new DateTime(2024, 1, 1, 0, 0, 0), DateTimeKind.Unspecified);
        var opts = OptionsWithConverter();

        var json = JsonSerializer.Serialize(value, opts);

        json.Should().Be("\"2024-01-01T00:00:00.000Z\"");
    }

    // ─────────────── Read (deserialize) ───────────────

    [Fact]
    public void Read_ParsesIso8601UtcString()
    {
        var opts = OptionsWithConverter();

        var result = JsonSerializer.Deserialize<DateTime>("\"2024-06-15T12:30:45.123Z\"", opts);

        result.Should().Be(new DateTime(2024, 6, 15, 12, 30, 45, 123, DateTimeKind.Utc));
    }

    [Fact]
    public void Read_ReturnsDateTimeWithUtcKind()
    {
        var opts = OptionsWithConverter();

        var result = JsonSerializer.Deserialize<DateTime>("\"2024-06-15T12:30:45.000Z\"", opts);

        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    // ─────────────── Round-trip ───────────────

    [Fact]
    public void RoundTrip_SerializeThenDeserialize_ProducesSameUtcValue()
    {
        var original = new DateTime(2024, 3, 22, 9, 15, 0, 500, DateTimeKind.Utc);
        var opts = OptionsWithConverter();

        var json = JsonSerializer.Serialize(original, opts);
        var result = JsonSerializer.Deserialize<DateTime>(json, opts);

        result.Should().Be(original);
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    // ─────────────── UtcNullableDateTimeJsonConverter — Write ───────────────

    [Fact]
    public void NullableWrite_SerializesNull_AsJsonNull()
    {
        DateTime? value = null;
        var opts = NullableOptionsWithConverter();

        var json = JsonSerializer.Serialize(value, opts);

        json.Should().Be("null");
    }

    [Fact]
    public void NullableWrite_SerializesNonNullUtcDateTime_WithZSuffix()
    {
        DateTime? value = new DateTime(2024, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);
        var opts = NullableOptionsWithConverter();

        var json = JsonSerializer.Serialize(value, opts);

        json.Should().Be("\"2024-12-31T23:59:59.999Z\"");
    }

    // ─────────────── UtcNullableDateTimeJsonConverter — Read ───────────────

    [Fact]
    public void NullableRead_ReturnsNull_ForJsonNull()
    {
        var opts = NullableOptionsWithConverter();

        var result = JsonSerializer.Deserialize<DateTime?>("null", opts);

        result.Should().BeNull();
    }

    [Fact]
    public void NullableRead_ReturnsUtcDateTime_ForNonNullValue()
    {
        var opts = NullableOptionsWithConverter();

        var result = JsonSerializer.Deserialize<DateTime?>("\"2024-06-01T08:00:00.000Z\"", opts);

        result.Should().NotBeNull();
        result!.Value.Kind.Should().Be(DateTimeKind.Utc);
        result.Value.Should().Be(new DateTime(2024, 6, 1, 8, 0, 0, DateTimeKind.Utc));
    }
}
