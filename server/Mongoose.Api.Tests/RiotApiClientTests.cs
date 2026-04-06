using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Mongoose.Api.Infrastructure;
using Mongoose.Api.Infrastructure.Riot;
using Xunit;

namespace Mongoose.Api.Tests;

/// <summary>
/// Unit tests for RiotApiClient — validates HTTP behavior, URL composition, response handling,
/// and cancellation propagation without calling real Riot endpoints.
/// </summary>
public class RiotApiClientTests
{
    private static readonly object _secretsLock = new();

    /// <summary>
    /// Initializes Secrets with a test API key so RiotUrlBuilder does not throw.
    /// In the Testing environment, Secrets allows re-initialization on each call.
    /// </summary>
    private static void EnsureSecretsInitialized()
    {
        lock (_secretsLock)
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RIOT_API_KEY"] = "test-api-key",
                    ["Security:EncryptionSecret"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-only-not-a-real-secret-1234"))
                })
                .Build();
            Secrets.Initialize(config);
        }
    }

    private static (RiotApiClient client, CapturingHttpMessageHandler handler) CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage>? responseFactory = null)
    {
        EnsureSecretsInitialized();
        responseFactory ??= _ => OkJson("{}");
        var handler = new CapturingHttpMessageHandler(responseFactory);
        var httpClient = new HttpClient(handler);
        var factory = new TestHttpClientFactory(httpClient);
        return (new RiotApiClient(factory), handler);
    }

    // ---- GetSummonerByPuuIdAsync ----

    [Fact]
    public async Task GetSummonerByPuuIdAsync_ReturnsEmptyObject_WhenSummonerNotFound()
    {
        // Arrange
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        using var result = await client.GetSummonerByPuuIdAsync("EUW", "test-puuid");

        // Assert — 404 returns empty JSON object, not an exception
        result.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
        result.RootElement.EnumerateObject().Should().BeEmpty();
    }

    [Fact]
    public async Task GetSummonerByPuuIdAsync_ReturnsJsonDocument_OnSuccess()
    {
        // Arrange
        var (client, _) = CreateClient(_ => OkJson("""{"profileIconId":123,"summonerLevel":100}"""));

        // Act
        using var result = await client.GetSummonerByPuuIdAsync("EUW", "test-puuid");

        // Assert
        result.RootElement.GetProperty("profileIconId").GetInt32().Should().Be(123);
        result.RootElement.GetProperty("summonerLevel").GetInt32().Should().Be(100);
    }

    [Fact]
    public async Task GetSummonerByPuuIdAsync_ThrowsHttpRequestException_OnServerError()
    {
        // Arrange
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        // Act
        var act = () => client.GetSummonerByPuuIdAsync("EUW", "test-puuid");

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetSummonerByPuuIdAsync_UrlContainsPuuid()
    {
        // Arrange
        var (client, handler) = CreateClient(_ => OkJson("{}"));

        // Act
        await client.GetSummonerByPuuIdAsync("EUW", "my-specific-puuid");

        // Assert
        handler.LastRequest!.RequestUri!.ToString().Should().Contain("my-specific-puuid");
    }

    // ---- GetLeagueEntriesByPuuidAsync ----

    [Fact]
    public async Task GetLeagueEntriesByPuuidAsync_ReturnsEmptyArray_WhenNotFound()
    {
        // Arrange
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        using var result = await client.GetLeagueEntriesByPuuidAsync("EUW", "test-puuid");

        // Assert — 404 returns empty array, not an exception
        result.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        result.RootElement.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetLeagueEntriesByPuuidAsync_ThrowsHttpRequestException_OnServerError()
    {
        // Arrange
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        // Act
        var act = () => client.GetLeagueEntriesByPuuidAsync("EUW", "test-puuid");

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ---- GetMatchHistoryAsync ----

    [Fact]
    public async Task GetMatchHistoryAsync_UrlIncludesStartCountParameters()
    {
        // Arrange
        var (client, handler) = CreateClient(_ => OkJson("[]"));

        // Act
        await client.GetMatchHistoryAsync("test-puuid", start: 5, count: 20);

        // Assert
        var url = handler.LastRequest!.RequestUri!.ToString();
        url.Should().Contain("start=5");
        url.Should().Contain("count=20");
    }

    [Fact]
    public async Task GetMatchHistoryAsync_UrlIncludesStartTime_WhenProvided()
    {
        // Arrange
        var (client, handler) = CreateClient(_ => OkJson("[]"));

        // Act
        await client.GetMatchHistoryAsync("test-puuid", startTime: 1700000000);

        // Assert
        handler.LastRequest!.RequestUri!.ToString().Should().Contain("startTime=1700000000");
    }

    [Fact]
    public async Task GetMatchHistoryAsync_UrlDoesNotIncludeStartTime_WhenNull()
    {
        // Arrange
        var (client, handler) = CreateClient(_ => OkJson("[]"));

        // Act
        await client.GetMatchHistoryAsync("test-puuid", startTime: null);

        // Assert
        handler.LastRequest!.RequestUri!.ToString().Should().NotContain("startTime");
    }

    [Fact]
    public async Task GetMatchHistoryAsync_UrlContainsMatchEndpoint()
    {
        // Arrange
        var (client, handler) = CreateClient(_ => OkJson("[]"));

        // Act
        await client.GetMatchHistoryAsync("my-puuid");

        // Assert
        var url = handler.LastRequest!.RequestUri!.ToString();
        url.Should().Contain("match/v5/matches");
        url.Should().Contain("my-puuid");
        url.Should().Contain("api_key=test-api-key");
    }

    [Fact]
    public async Task GetMatchHistoryAsync_ThrowsHttpRequestException_OnServerError()
    {
        // Arrange
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        // Act
        var act = () => client.GetMatchHistoryAsync("test-puuid");

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ---- GetPuuIdAsync ----

    [Fact]
    public async Task GetPuuIdAsync_ParsesPuuidFieldFromJson()
    {
        // Arrange
        var (client, _) = CreateClient(_ => OkJson("""{"puuid":"expected-puuid","gameName":"Player","tagLine":"NA1"}"""));

        // Act
        var puuid = await client.GetPuuIdAsync("Player", "NA1");

        // Assert
        puuid.Should().Be("expected-puuid");
    }

    [Fact]
    public async Task GetPuuIdAsync_ThrowsHttpRequestException_OnNon200Response()
    {
        // Arrange
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var act = () => client.GetPuuIdAsync("Player", "NA1");

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetPuuIdAsync_ThrowsInvalidOperationException_WhenPuuidFieldMissing()
    {
        // Arrange — response is valid JSON but has no "puuid" field
        var (client, _) = CreateClient(_ => OkJson("""{"gameName":"Player"}"""));

        // Act
        var act = () => client.GetPuuIdAsync("Player", "NA1");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ---- GetMatchInfoAsync ----

    [Fact]
    public async Task GetMatchInfoAsync_ThrowsHttpRequestException_OnServerError()
    {
        // Arrange
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        // Act
        var act = () => client.GetMatchInfoAsync("EUW1_12345");

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ---- Cancellation ----

    [Fact]
    public async Task GetMatchHistoryAsync_ThrowsOperationCancelledException_WhenTokenAlreadyCancelled()
    {
        // Arrange
        var (client, _) = CreateClient(_ => OkJson("[]"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => client.GetMatchHistoryAsync("test-puuid", ct: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetSummonerByPuuIdAsync_ThrowsOperationCancelledException_WhenTokenAlreadyCancelled()
    {
        // Arrange
        var (client, _) = CreateClient(_ => OkJson("{}"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => client.GetSummonerByPuuIdAsync("EUW", "test-puuid", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ---- helpers ----

    private static HttpResponseMessage OkJson(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    internal sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public HttpRequestMessage? LastRequest { get; private set; }

        public CapturingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
            => _responseFactory = responseFactory;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastRequest = request;
            return Task.FromResult(_responseFactory(request));
        }
    }

    internal sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public TestHttpClientFactory(HttpClient client) => _client = client;

        public HttpClient CreateClient(string name) => _client;
    }
}
