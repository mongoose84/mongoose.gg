using System.Net;
using System.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Xunit;

namespace Mongoose.Api.Tests;

public class GoogleSignOnEndpointTests
{
    /// <summary>
    /// Composes the identity-providers lookup with the users lookup, mirroring how
    /// GoogleSignOnEndpoint resolves a user from a Google Sign-On identity.
    /// </summary>
    private static async Task<User?> GetUserByGoogleIdAsync(TestWebApplicationFactory factory, string googleId)
    {
        var userId = await factory.IdentityProvidersRepository.GetUserIdByProviderIdentityAsync("google", googleId);
        return userId.HasValue ? await factory.UsersRepository.GetByIdAsync(userId.Value) : null;
    }

    private static TestWebApplicationFactory CreateEnabledFactory(IDictionary<string, string?>? extraOverrides = null)
    {
        var overrides = new Dictionary<string, string?>
        {
            ["Auth:EnableGoogleSignOn"] = "true",
            ["Auth:Google:ClientId"] = "test-client-id",
            ["Auth:Google:RedirectUri"] = "http://localhost/api/v2/auth/google/callback",
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
    /// Starts the GSO flow and returns the state parameter Google would echo back.
    /// The state cookie is captured by the client's cookie container.
    /// </summary>
    private static async Task<string> StartLoginAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v2/auth/google/login");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var state = HttpUtility.ParseQueryString(response.Headers.Location!.Query)["state"];
        state.Should().NotBeNullOrEmpty();
        return state!;
    }

    [Fact]
    public async Task Google_login_redirects_back_with_error_when_disabled()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = CreateBrowserClient(factory);

        var response = await client.GetAsync("/api/v2/auth/google/login");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/auth?error=google_signon_disabled");
    }

    [Fact]
    public async Task Google_login_redirects_to_google_authorize_and_sets_state_cookie()
    {
        using var factory = CreateEnabledFactory();
        using var client = CreateBrowserClient(factory);

        var response = await client.GetAsync("/api/v2/auth/google/login");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = response.Headers.Location!.ToString();
        location.Should().StartWith("https://accounts.google.com/o/oauth2/v2/auth");
        location.Should().Contain("client_id=test-client-id");
        location.Should().Contain("response_type=code");
        location.Should().Contain("redirect_uri=");
        location.Should().Contain("state=");

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        var stateCookie = cookies!.Single(c => c.StartsWith("mongoose-gso-state"));
        stateCookie.Should().ContainEquivalentOf("httponly");
        stateCookie.Should().ContainEquivalentOf("samesite=lax", "callback arrives as a cross-site top-level navigation");
    }

    [Fact]
    public async Task Callback_rejects_state_mismatch_without_signing_in()
    {
        using var factory = CreateEnabledFactory();
        using var client = CreateBrowserClient(factory);
        factory.GoogleSignOnClient.MapCode("code-1", new GoogleSignOnIdentity("google-1", "player1@gmail.com", true, "Player One"));

        await StartLoginAsync(client);
        var response = await client.GetAsync("/api/v2/auth/google/callback?code=code-1&state=not-the-right-state");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/auth?error=google_signon_state");
        (await GetUserByGoogleIdAsync(factory, "google-1")).Should().BeNull();
    }

    [Fact]
    public async Task Callback_without_prior_login_start_is_rejected()
    {
        using var factory = CreateEnabledFactory();
        using var client = CreateBrowserClient(factory);

        var response = await client.GetAsync("/api/v2/auth/google/callback?code=code-1&state=any-state");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/auth?error=google_signon_state");
    }

    [Fact]
    public async Task Callback_redirects_denied_when_google_reports_error()
    {
        using var factory = CreateEnabledFactory();
        using var client = CreateBrowserClient(factory);

        await StartLoginAsync(client);
        var response = await client.GetAsync("/api/v2/auth/google/callback?error=access_denied");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/auth?error=google_signon_denied");
    }

    [Fact]
    public async Task Callback_creates_user_and_signs_in_when_no_existing_account()
    {
        using var factory = CreateEnabledFactory();
        using var client = CreateBrowserClient(factory);
        factory.GoogleSignOnClient.MapCode("code-new", new GoogleSignOnIdentity("google-new", "newplayer@gmail.com", true, "New Player"));

        var state = await StartLoginAsync(client);
        var response = await client.GetAsync($"/api/v2/auth/google/callback?code=code-new&state={state}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/app/overview");

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(c => c.StartsWith("mongoose-auth"), "a session cookie must be issued");

        var user = await GetUserByGoogleIdAsync(factory, "google-new");
        user.Should().NotBeNull();
        user!.Username.Should().Be("newplayer");
        user.Email.Should().Be("newplayer@gmail.com");
        user.EmailVerified.Should().BeTrue("Google reported the email as verified");
        user.IsActive.Should().BeTrue();
        user.Tier.Should().Be("free");
    }

    [Fact]
    public async Task Callback_signs_in_existing_google_user_without_creating_a_new_one()
    {
        using var factory = CreateEnabledFactory();
        using var client = CreateBrowserClient(factory);
        factory.UsersRepository.AddGoogleSignOnUser("returning-player", "google-return");
        factory.GoogleSignOnClient.MapCode("code-return", new GoogleSignOnIdentity("google-return", "returning@gmail.com", true, "Returning Player"));
        var userCountBefore = await factory.UsersRepository.GetActiveUserCountAsync();

        var state = await StartLoginAsync(client);
        var response = await client.GetAsync($"/api/v2/auth/google/callback?code=code-return&state={state}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/app/overview");
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(c => c.StartsWith("mongoose-auth"));

        var user = await GetUserByGoogleIdAsync(factory, "google-return");
        user!.Username.Should().Be("returning-player");
        user.LastLoginAt.Should().NotBeNull();
        (await factory.UsersRepository.GetActiveUserCountAsync()).Should().Be(userCountBefore);
    }

    [Fact]
    public async Task Callback_auto_links_to_existing_local_account_with_matching_verified_email()
    {
        using var factory = CreateEnabledFactory();
        using var client = CreateBrowserClient(factory);
        factory.UsersRepository.AddUnverifiedUser("localuser", "shared@gmail.com", "password123");
        factory.GoogleSignOnClient.MapCode("code-link", new GoogleSignOnIdentity("google-link", "shared@gmail.com", true, "Shared Account"));
        var userCountBefore = await factory.UsersRepository.GetActiveUserCountAsync();

        var state = await StartLoginAsync(client);
        var response = await client.GetAsync($"/api/v2/auth/google/callback?code=code-link&state={state}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/app/overview");

        var user = await GetUserByGoogleIdAsync(factory, "google-link");
        user.Should().NotBeNull("the Google identity should be linked to the existing local account");
        user!.Username.Should().Be("localuser");
        user.EmailVerified.Should().BeTrue("Google verifying the email promotes the local account to verified");
        (await factory.UsersRepository.GetActiveUserCountAsync()).Should().Be(userCountBefore, "no new account should be created");
    }

    [Fact]
    public async Task Callback_does_not_auto_link_when_google_email_is_unverified()
    {
        using var factory = CreateEnabledFactory();
        using var client = CreateBrowserClient(factory);
        factory.UsersRepository.AddUnverifiedUser("localuser2", "unverified-shared@gmail.com", "password123");
        factory.GoogleSignOnClient.MapCode("code-unverified", new GoogleSignOnIdentity("google-unverified", "unverified-shared@gmail.com", false, "Unverified"));
        var userCountBefore = await factory.UsersRepository.GetActiveUserCountAsync();

        var state = await StartLoginAsync(client);
        var response = await client.GetAsync($"/api/v2/auth/google/callback?code=code-unverified&state={state}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/app/overview");

        var googleUser = await GetUserByGoogleIdAsync(factory, "google-unverified");
        googleUser.Should().NotBeNull();
        googleUser!.Username.Should().NotBe("localuser2", "an unverified Google email must not hijack an existing account");
        (await factory.UsersRepository.GetActiveUserCountAsync()).Should().Be(userCountBefore + 1, "a distinct new account is created instead");
    }

    [Fact]
    public async Task Callback_redirects_failed_when_code_exchange_fails()
    {
        using var factory = CreateEnabledFactory();
        using var client = CreateBrowserClient(factory);

        var state = await StartLoginAsync(client);
        var response = await client.GetAsync($"/api/v2/auth/google/callback?code=unknown-code&state={state}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/auth?error=google_signon_failed");
    }

    [Fact]
    public async Task Callback_rejects_inactive_user()
    {
        using var factory = CreateEnabledFactory();
        using var client = CreateBrowserClient(factory);
        factory.UsersRepository.AddGoogleSignOnUser("banned-player", "google-inactive", isActive: false);
        factory.GoogleSignOnClient.MapCode("code-inactive", new GoogleSignOnIdentity("google-inactive", "banned@gmail.com", true, "Banned"));

        var state = await StartLoginAsync(client);
        var response = await client.GetAsync($"/api/v2/auth/google/callback?code=code-inactive&state={state}");

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
            ["Auth:Google:ClientBaseUrl"] = "http://localhost:5174/"
        });
        using var client = CreateBrowserClient(factory);
        factory.GoogleSignOnClient.MapCode("code-spa", new GoogleSignOnIdentity("google-spa", "spauser@gmail.com", true, "SpaUser"));

        var state = await StartLoginAsync(client);
        var response = await client.GetAsync($"/api/v2/auth/google/callback?code=code-spa&state={state}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("http://localhost:5174/app/overview");
    }
}
