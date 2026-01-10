using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RiotProxy.Tests;

[Collection("EnvIsolation")]
public class DiagnosticsEndpointTests
{
    private static async Task<string> LoginAndGetAuthCookieAsync(TestWebApplicationFactory factory)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.PostAsJsonAsync("/api/v2/login", new { username = "tester", password = "dev-secret" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        var cookie = cookies!.First();
        var authCookie = cookie.Split(';', 2)[0]; // name=value
        return authCookie;
    }

    [Fact]
    public async Task Diagnostics_requires_authentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v1.0/diagnostics");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Diagnostics_returns_configuration_status_when_authenticated()
    {
        using var env = EnvironmentVariableScope.Set(
            ("RIOT_API_KEY", "test-key"),
            ("LOL_DB_CONNECTIONSTRING", "Server=localhost;Port=3306;Database=test;User Id=test;Password=test;"),
            ("LOL_DB_CONNECTIONSTRING_V2", "Server=localhost;Port=3306;Database=test;User Id=test;Password=test;")
        );
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1.0/diagnostics");
        req.Headers.Add("Cookie", authCookie);
        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<DiagnosticsResponse>();
        payload.Should().NotBeNull();
        payload!.configuration.apiKeyConfigured.Should().BeTrue();
        payload.configuration.databaseConfigured.Should().BeTrue();
        payload.configuration.databaseV2Configured.Should().BeTrue();
        payload.configuration.allConfigured.Should().BeTrue();
        payload.metrics.metricHits.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task Diagnostics_reports_missing_api_key()
    {
        using var factory = new TestWebApplicationFactory(new Dictionary<string, string?>
        {
            ["RIOT_API_KEY"] = string.Empty
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1.0/diagnostics");
        req.Headers.Add("Cookie", authCookie);
        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<DiagnosticsResponse>();
        payload!.configuration.apiKeyConfigured.Should().BeFalse();
        payload.configuration.allConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task Diagnostics_reports_missing_database_connection()
    {
        using var factory = new TestWebApplicationFactory(new Dictionary<string, string?>
        {
            ["LOL_DB_CONNECTIONSTRING"] = string.Empty
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1.0/diagnostics");
        req.Headers.Add("Cookie", authCookie);
        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<DiagnosticsResponse>();
        payload!.configuration.databaseConfigured.Should().BeFalse();
        payload.configuration.allConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task Diagnostics_reports_missing_database_v2_connection()
    {
        using var factory = new TestWebApplicationFactory(new Dictionary<string, string?>
        {
            ["LOL_DB_CONNECTIONSTRING_V2"] = string.Empty
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var authCookie = await LoginAndGetAuthCookieAsync(factory);
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1.0/diagnostics");
        req.Headers.Add("Cookie", authCookie);
        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<DiagnosticsResponse>();
        payload!.configuration.databaseV2Configured.Should().BeFalse();
        payload.configuration.allConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task Diagnostics_metricHits_increments_on_each_call()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var authCookie = await LoginAndGetAuthCookieAsync(factory);

        var req1 = new HttpRequestMessage(HttpMethod.Get, "/api/v1.0/diagnostics");
        req1.Headers.Add("Cookie", authCookie);
        var res1 = await client.SendAsync(req1);
        var p1 = await res1.Content.ReadFromJsonAsync<DiagnosticsResponse>();
        var m1 = p1!.metrics.metricHits;

        var req2 = new HttpRequestMessage(HttpMethod.Get, "/api/v1.0/diagnostics");
        req2.Headers.Add("Cookie", authCookie);
        var res2 = await client.SendAsync(req2);
        var p2 = await res2.Content.ReadFromJsonAsync<DiagnosticsResponse>();
        var m2 = p2!.metrics.metricHits;

        m2.Should().Be(m1 + 1);
    }

    public record DiagnosticsResponse(string environment, DateTime timestamp, string build, Configuration configuration, Metrics metrics, string[] notes);
    public record Configuration(bool apiKeyConfigured, bool databaseConfigured, bool databaseV2Configured, bool allConfigured);
    public record Metrics(long homeHits, long metricHits, long winrateHits, long summonerHits, string lastUrlCalled);
}
