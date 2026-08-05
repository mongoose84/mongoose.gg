using System.Net;
using System.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Mongoose.Api.Core.Interfaces;
using Xunit;

namespace Mongoose.Api.Tests;

public class RiotSignOnEndpointTests
{
    private static TestWebApplicationFactory CreateEnabledFactory(IDictionary<string, string?>? extraOverrides = null)
    {
        var overrides = new Dictionary<string, string?>
        {
            ["Auth:EnableRiotSignOn"] = "true",
            ["Auth:Riot:ClientId"] = "test-client-id",
            ["Auth:Riot:RedirectUri"] = "http://localhost/api/v2/auth/riot/callback",
            ["RateLimiting:Enabled"] = "false"
        };

        if (extraOverrides != null)
        {
            foreach (var kvp in extraOverrides)
            {
                overrides[kvp.Key] = kvp.Value;
            }
        }

        return new TestWebApplicationFactory(overrides);
    }

    private static HttpClient CreateBrowserClient(TestWebApplicationFactory factory)
    {
        // AllowAutoRedirect=false so tests can assert on the 302 Location targets;
        // HandleCookies stays on (default) so the state cookie round-trips like a browser.
        return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    /// <summary>
    /// Starts the RSO flow and returns the state parameter Riot would echo back.
    /// The state cookie is captured by the client's cookie container.
    /// </summary>
    private static async Task<string> StartLoginAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v2/auth/riot/login");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var state = HttpUtility.ParseQueryString(response.Headers.Location!.Query)["state"];
        state.Should().NotBeNullOrEmpty();
        return state!;
    }

    [Fact]
    public async Task Riot_login_redirects_back_with_error_when_disabled()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateBrowserClient(factory);

        var response = await client.GetAsync("/api/v2/auth/riot/login");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/auth?error=riot_signon_disabled");
    }

    [Fact]
    public async Task Riot_login_redirects_to_riot_authorize_and_sets_state_cookie()
    {
        using var factory = CreateEnabledFactory();
        using var client = CreateBrowserClient(factory);

        var response = await client.GetAsync("/api/v2/auth/riot/login");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = response.Headers.Location!.ToString();
        location.Should().StartWith("https://auth.riotgames.com/authorize");
        location.Should().Contain("client_id=test-client-id");
        location.Should().Contain("response_type=code");
        location.Should().Contain("redirect_uri=");
        location.Should().Contain("state=");

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        var stateCookie = cookies!.Single(c => c.StartsWith("mongoose-rso-state"));
        stateCookie.Should().ContainEquivalentOf("httponly");
        stateCookie.Should().ContainEquivalentOf("samesite=lax", "callback arrives as a cross-site top-level navigation");
    }

    [Fact]
    public async Task Callback_rejects_state_mismatch_without_signing_in()
    {
        using var factory = CreateEnabledFactory();
        using var client = CreateBrowserClient(factory);
        factory.RiotSignOnClient.MapCode("code-1", new RiotSignOnIdentity("puuid-rso-1", "Faker", "KR1", "kr"));

        await StartLoginAsync(client);
        var response = await client.GetAsync("/api/v2/auth/riot/callback?code=code-1&state=not-the-right-state");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/auth?error=riot_signon_state");
        (await factory.UsersRepository.GetByRiotPuuidAsync("puuid-rso-1")).Should().BeNull();
    }

    [Fact]
    public async Task Callback_without_prior_login_start_is_rejected()
    {
        using var factory = CreateEnabledFactory();
        using var client = CreateBrowserClient(factory);

        var response = await client.GetAsync("/api/v2/auth/riot/callback?code=code-1&state=any-state");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/auth?error=riot_signon_state");
    }

    [Fact]
    public async Task Callback_redirects_denied_when_riot_reports_error()
    {
        using var factory = CreateEnabledFactory();
        using var client = CreateBrowserClient(factory);

        await StartLoginAsync(client);
        var response = await client.GetAsync("/api/v2/auth/riot/callback?error=access_denied");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/auth?error=riot_signon_denied");
    }

    [Fact]
    public async Task Callback_creates_user_links_riot_account_and_signs_in()
    {
        using var factory = CreateEnabledFactory();
        using var client = CreateBrowserClient(factory);
        factory.RiotSignOnClient.MapCode("code-new", new RiotSignOnIdentity("puuid-rso-new", "Faker", "KR1", "kr"));

        var state = await StartLoginAsync(client);
        var response = await client.GetAsync($"/api/v2/auth/riot/callback?code=code-new&state={state}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/app/overview");

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(c => c.StartsWith("mongoose-auth"), "a session cookie must be issued");

        var user = await factory.UsersRepository.GetByRiotPuuidAsync("puuid-rso-new");
        user.Should().NotBeNull();
        user!.Username.Should().Be("faker");
        user.EmailVerified.Should().BeTrue("RSO users have no email to verify");
        user.IsActive.Should().BeTrue();
        user.Tier.Should().Be("free");

        var account = await factory.RiotAccountsRepository.GetByPuuidAsync("puuid-rso-new");
        account.Should().NotBeNull("the Riot account is linked automatically — identity comes from Riot");
        account!.GameName.Should().Be("Faker");
        account.TagLine.Should().Be("KR1");
        account.Region.Should().Be("kr");
        account.SyncStatus.Should().Be("pending");

        var primary = await factory.UserRiotAccountsRepository.GetPrimaryByUserIdAsync(user.UserId);
        primary.Should().NotBeNull();
        primary!.Value.Link.Puuid.Should().Be("puuid-rso-new");
    }

    [Fact]
    public async Task Callback_signs_in_existing_user_without_creating_a_new_one()
    {
        using var factory = CreateEnabledFactory();
        using var client = CreateBrowserClient(factory);
        factory.UsersRepository.AddRiotSignOnUser("returning-player", "puuid-rso-返");
        factory.RiotSignOnClient.MapCode("code-return", new RiotSignOnIdentity("puuid-rso-返", "Returning Player", "EUW", "euw1"));
        var userCountBefore = await factory.UsersRepository.GetActiveUserCountAsync();

        var state = await StartLoginAsync(client);
        var response = await client.GetAsync($"/api/v2/auth/riot/callback?code=code-return&state={state}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/app/overview");
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(c => c.StartsWith("mongoose-auth"));

        var user = await factory.UsersRepository.GetByRiotPuuidAsync("puuid-rso-返");
        user!.Username.Should().Be("returning-player");
        user.LastLoginAt.Should().NotBeNull();
        (await factory.UsersRepository.GetActiveUserCountAsync()).Should().Be(userCountBefore);
    }

    [Fact]
    public async Task Callback_redirects_failed_when_code_exchange_fails()
    {
        using var factory = CreateEnabledFactory();
        using var client = CreateBrowserClient(factory);

        var state = await StartLoginAsync(client);
        var response = await client.GetAsync($"/api/v2/auth/riot/callback?code=unknown-code&state={state}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/auth?error=riot_signon_failed");
    }

    [Fact]
    public async Task Callback_rejects_inactive_user()
    {
        using var factory = CreateEnabledFactory();
        using var client = CreateBrowserClient(factory);
        factory.UsersRepository.AddRiotSignOnUser("banned-player", "puuid-rso-inactive", isActive: false);
        factory.RiotSignOnClient.MapCode("code-inactive", new RiotSignOnIdentity("puuid-rso-inactive", "Banned", "EUW", "euw1"));

        var state = await StartLoginAsync(client);
        var response = await client.GetAsync($"/api/v2/auth/riot/callback?code=code-inactive&state={state}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/auth?error=account_deactivated");
        response.Headers.TryGetValues("Set-Cookie", out var cookies);
        (cookies ?? Enumerable.Empty<string>()).Should().NotContain(c => c.StartsWith("mongoose-auth"));
    }

    [Fact]
    public async Task Callback_respects_client_base_url_for_redirects()
    {
        using var factory = CreateEnabledFactory(new Dictionary<string, string?>
        {
            ["Auth:Riot:ClientBaseUrl"] = "http://localhost:5174/"
        });
        using var client = CreateBrowserClient(factory);
        factory.RiotSignOnClient.MapCode("code-spa", new RiotSignOnIdentity("puuid-rso-spa", "SpaUser", "EUW", "euw1"));

        var state = await StartLoginAsync(client);
        var response = await client.GetAsync($"/api/v2/auth/riot/callback?code=code-spa&state={state}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("http://localhost:5174/app/overview");
    }
}
