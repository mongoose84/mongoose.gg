using System.Net;
using System.Net.Http.Json;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Mongoose.Api.Tests;

public class RiotAccountsEndpointTests
{
    private static async Task<string> LoginAndGetCookieAsync(
        TestWebApplicationFactory factory,
        string username = "tester",
        string password = "test-password")
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var loginResponse = await client.PostAsJsonAsync("/api/v2/auth/login", new { username, password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return AuthCookieTestHelper.GetAuthCookie(loginResponse);
    }

    [Fact]
    public async Task SetPrimary_Returns200_AndUpdatesPrimary_WhenAccountIsLinked()
    {
        using var factory = new TestWebApplicationFactory();
        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-primary-old", "Main", "na1", "Main#NA1", 100, 1);
        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-target", "Smurf", "na1", "Smurf#NA1", 100, 1);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-primary-old", isPrimary: true);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-target", isPrimary: false);

        var cookie = await LoginAndGetCookieAsync(factory);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/v2/users/me/riot-accounts/puuid-target/primary");
        request.Headers.Add("Cookie", cookie);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();

        var links = await factory.UserRiotAccountsRepository.GetByUserIdAsync(1);
        links.Single(l => l.Link.Puuid == "puuid-target").Link.IsPrimary.Should().BeTrue();
        links.Single(l => l.Link.Puuid == "puuid-primary-old").Link.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public async Task SetPrimary_Returns404_WhenAccountIsNotLinked()
    {
        using var factory = new TestWebApplicationFactory();
        var cookie = await LoginAndGetCookieAsync(factory);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/v2/users/me/riot-accounts/not-linked/primary");
        request.Headers.Add("Cookie", cookie);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("code").GetString().Should().Be("ACCOUNT_NOT_LINKED");
    }

    [Fact]
    public async Task SetPrimary_Returns401_WhenUnauthenticated()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PutAsync("/api/v2/users/me/riot-accounts/any/primary", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnlinkPrimary_PromotesNextOldestAccountToPrimary()
    {
        using var factory = new TestWebApplicationFactory();
        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-primary", "Main", "na1", "Main#NA1", 100, 1);
        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-second", "Second", "na1", "Second#NA1", 100, 1);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-primary", isPrimary: true);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-second", isPrimary: false);

        var cookie = await LoginAndGetCookieAsync(factory);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v2/users/me/riot-accounts/puuid-primary");
        request.Headers.Add("Cookie", cookie);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var links = await factory.UserRiotAccountsRepository.GetByUserIdAsync(1);
        links.Should().HaveCount(1);
        links[0].Link.Puuid.Should().Be("puuid-second");
        links[0].Link.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task LinkAccount_Returns400_WhenFreeTierAlreadyAtLimit()
    {
        using var factory = new TestWebApplicationFactory();
        factory.UsersRepository.SetTier("tester", "free");
        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-existing", "Main", "na1", "Main#NA1", 100, 1);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-existing", isPrimary: true);

        var cookie = await LoginAndGetCookieAsync(factory);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/users/me/riot-accounts")
        {
            Content = JsonContent.Create(new { gameName = "Another", tagLine = "EUW", region = "euw1" })
        };
        request.Headers.Add("Cookie", cookie);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("code").GetString().Should().Be("ACCOUNT_LIMIT_REACHED");
    }

    [Fact]
    public async Task LinkAccount_Returns200_WhenFreeTierAtLimit_AndAccountAlreadyLinked()
    {
        using var factory = new TestWebApplicationFactory();
        factory.UsersRepository.SetTier("tester", "free");
        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-existing", "Main", "na1", "Main#NA1", 100, 1);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-existing", isPrimary: true);
        factory.RiotApiClient.MapRiotIdToPuuid("Main", "NA1", "puuid-existing");

        var cookie = await LoginAndGetCookieAsync(factory);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/users/me/riot-accounts")
        {
            Content = JsonContent.Create(new { gameName = "Main", tagLine = "NA1", region = "na1" })
        };
        request.Headers.Add("Cookie", cookie);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("puuid").GetString().Should().Be("puuid-existing");
        json.RootElement.GetProperty("isPrimary").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task UsersMe_ReturnsPrimaryOnly_WhenTierIsFree()
    {
        using var factory = new TestWebApplicationFactory();
        factory.UsersRepository.SetTier("tester", "free");
        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-primary", "Main", "na1", "Main#NA1", 100, 1);
        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-second", "Smurf", "na1", "Smurf#NA1", 100, 1);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-primary", isPrimary: true);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-second", isPrimary: false);

        var cookie = await LoginAndGetCookieAsync(factory);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v2/users/me");
        request.Headers.Add("Cookie", cookie);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var riotAccounts = json.RootElement.GetProperty("riotAccounts");
        riotAccounts.GetArrayLength().Should().Be(1);
        riotAccounts.EnumerateArray().First().GetProperty("puuid").GetString().Should().Be("puuid-primary");
    }

    [Fact]
    public async Task UsersMe_ReturnsAllLinkedAccounts_WhenTierIsPro()
    {
        using var factory = new TestWebApplicationFactory();
        factory.UsersRepository.SetTier("tester", "pro");
        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-primary", "Main", "na1", "Main#NA1", 100, 1);
        factory.RiotAccountsRepository.AddRiotAccount(1, "puuid-second", "Smurf", "na1", "Smurf#NA1", 100, 1);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-primary", isPrimary: true);
        factory.UserRiotAccountsRepository.LinkAccount(1, "puuid-second", isPrimary: false);

        var cookie = await LoginAndGetCookieAsync(factory);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v2/users/me");
        request.Headers.Add("Cookie", cookie);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var riotAccounts = json.RootElement.GetProperty("riotAccounts");
        riotAccounts.GetArrayLength().Should().Be(2);
    }
}
