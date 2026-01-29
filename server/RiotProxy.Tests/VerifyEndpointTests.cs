using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using RiotProxy.Core.Entities;
using Xunit;

namespace RiotProxy.Tests;

public class VerifyEndpointTests
{
    private static async Task<(string cookie, long userId)> LoginUnverifiedUserAsync(TestWebApplicationFactory factory)
    {
        // Add an unverified user
        factory.UsersRepository.AddUnverifiedUser("unverified", "unverified@test.com", "test-password");
        
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.PostAsJsonAsync("/api/v2/auth/login", new { username = "unverified", password = "test-password" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        var cookie = cookies!.First().Split(';', 2)[0];
        
        // Get the user ID for adding tokens
        var user = await factory.UsersRepository.GetByUsernameAsync("unverified");
        return (cookie, user!.UserId);
    }

    [Fact]
    public async Task Verify_requires_authentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/verify", new { code = "123456" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Verify_rejects_invalid_code_format()
    {
        using var factory = new TestWebApplicationFactory();
        var (cookie, userId) = await LoginUnverifiedUserAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/verify")
        {
            Content = JsonContent.Create(new { code = "abc" })
        };
        req.Headers.Add("Cookie", cookie);
        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("INVALID_CODE_FORMAT");
    }

    [Fact]
    public async Task Verify_rejects_when_no_token_exists()
    {
        using var factory = new TestWebApplicationFactory();
        var (cookie, userId) = await LoginUnverifiedUserAsync(factory);
        // No token added for this user

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/verify")
        {
            Content = JsonContent.Create(new { code = "123456" })
        };
        req.Headers.Add("Cookie", cookie);
        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("NO_CODE_STORED");
    }

    [Fact]
    public async Task Verify_rejects_wrong_code()
    {
        using var factory = new TestWebApplicationFactory();
        var (cookie, userId) = await LoginUnverifiedUserAsync(factory);
        factory.TokensRepository.AddToken(userId, TokenTypes.EmailVerification, "123456", DateTime.UtcNow.AddMinutes(15));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/verify")
        {
            Content = JsonContent.Create(new { code = "654321" })
        };
        req.Headers.Add("Cookie", cookie);
        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("INVALID_CODE");
    }

    [Fact]
    public async Task Verify_succeeds_with_correct_code()
    {
        using var factory = new TestWebApplicationFactory();
        var (cookie, userId) = await LoginUnverifiedUserAsync(factory);
        factory.TokensRepository.AddToken(userId, TokenTypes.EmailVerification, "123456", DateTime.UtcNow.AddMinutes(15));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/verify")
        {
            Content = JsonContent.Create(new { code = "123456" })
        };
        req.Headers.Add("Cookie", cookie);
        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<VerifyResponse>();
        body!.Verified.Should().BeTrue();
        
        // Verify the user is now marked as verified
        var user = await factory.UsersRepository.GetByIdAsync(userId);
        user!.EmailVerified.Should().BeTrue();
    }

    [Fact]
    public async Task Verify_marks_token_as_used_on_success()
    {
        using var factory = new TestWebApplicationFactory();
        var (cookie, userId) = await LoginUnverifiedUserAsync(factory);
        factory.TokensRepository.AddToken(userId, TokenTypes.EmailVerification, "123456", DateTime.UtcNow.AddMinutes(15));

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/verify")
        {
            Content = JsonContent.Create(new { code = "123456" })
        };
        req.Headers.Add("Cookie", cookie);
        await client.SendAsync(req);

        // Verify the token was marked as used
        var tokens = factory.TokensRepository.GetAllTokensForUser(userId);
        tokens.Should().OnlyContain(t => t.UsedAt.HasValue);
    }

    [Fact]
    public async Task Verify_increments_attempts_on_wrong_code()
    {
        using var factory = new TestWebApplicationFactory();
        var (cookie, userId) = await LoginUnverifiedUserAsync(factory);
        factory.TokensRepository.AddToken(userId, TokenTypes.EmailVerification, "123456", DateTime.UtcNow.AddMinutes(15));
        var tokenId = factory.TokensRepository.GetAllTokensForUser(userId).First().Id;

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/verify")
        {
            Content = JsonContent.Create(new { code = "654321" })
        };
        req.Headers.Add("Cookie", cookie);
        await client.SendAsync(req);

        // Verify the attempt counter was incremented
        var token = factory.TokensRepository.GetToken(tokenId);
        token!.Attempts.Should().Be(1);
    }

    [Fact]
    public async Task Verify_rejects_when_max_attempts_exceeded()
    {
        using var factory = new TestWebApplicationFactory();
        var (cookie, userId) = await LoginUnverifiedUserAsync(factory);
        factory.TokensRepository.AddToken(userId, TokenTypes.EmailVerification, "123456", DateTime.UtcNow.AddMinutes(15));
        var tokenId = factory.TokensRepository.GetAllTokensForUser(userId).First().Id;

        // Set attempts to max (default is 5)
        factory.TokensRepository.SetTokenAttempts(tokenId, 5);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/verify")
        {
            Content = JsonContent.Create(new { code = "123456" })
        };
        req.Headers.Add("Cookie", cookie);
        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("MAX_ATTEMPTS_EXCEEDED");

        // Verify the token was invalidated
        var token = factory.TokensRepository.GetToken(tokenId);
        token!.UsedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Verify_invalidates_token_after_max_attempts_even_with_correct_code()
    {
        using var factory = new TestWebApplicationFactory();
        var (cookie, userId) = await LoginUnverifiedUserAsync(factory);
        factory.TokensRepository.AddToken(userId, TokenTypes.EmailVerification, "123456", DateTime.UtcNow.AddMinutes(15));
        var tokenId = factory.TokensRepository.GetAllTokensForUser(userId).First().Id;

        // Set attempts to max (default is 5)
        factory.TokensRepository.SetTokenAttempts(tokenId, 5);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/verify")
        {
            Content = JsonContent.Create(new { code = "123456" }) // Correct code
        };
        req.Headers.Add("Cookie", cookie);
        var response = await client.SendAsync(req);

        // Should still reject even with correct code
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("MAX_ATTEMPTS_EXCEEDED");

        // User should still be unverified
        var user = await factory.UsersRepository.GetByIdAsync(userId);
        user!.EmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task Verify_allows_attempts_below_max()
    {
        using var factory = new TestWebApplicationFactory();
        var (cookie, userId) = await LoginUnverifiedUserAsync(factory);
        factory.TokensRepository.AddToken(userId, TokenTypes.EmailVerification, "123456", DateTime.UtcNow.AddMinutes(15));
        var tokenId = factory.TokensRepository.GetAllTokensForUser(userId).First().Id;

        // Set attempts to 4 (below max of 5)
        factory.TokensRepository.SetTokenAttempts(tokenId, 4);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/verify")
        {
            Content = JsonContent.Create(new { code = "123456" })
        };
        req.Headers.Add("Cookie", cookie);
        var response = await client.SendAsync(req);

        // Should succeed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<VerifyResponse>();
        body!.Verified.Should().BeTrue();
    }

    [Fact]
    public async Task Verify_respects_custom_max_attempts_config()
    {
        var overrides = new Dictionary<string, string?>
        {
            ["Auth:VerificationMaxAttempts"] = "3"
        };
        using var factory = new TestWebApplicationFactory(overrides);
        var (cookie, userId) = await LoginUnverifiedUserAsync(factory);
        factory.TokensRepository.AddToken(userId, TokenTypes.EmailVerification, "123456", DateTime.UtcNow.AddMinutes(15));
        var tokenId = factory.TokensRepository.GetAllTokensForUser(userId).First().Id;

        // Set attempts to 3 (custom max)
        factory.TokensRepository.SetTokenAttempts(tokenId, 3);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/verify")
        {
            Content = JsonContent.Create(new { code = "123456" })
        };
        req.Headers.Add("Cookie", cookie);
        var response = await client.SendAsync(req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body!.Code.Should().Be("MAX_ATTEMPTS_EXCEEDED");
    }

    [Fact]
    public async Task Verify_updates_user_before_marking_token_as_used()
    {
        using var factory = new TestWebApplicationFactory();
        var (cookie, userId) = await LoginUnverifiedUserAsync(factory);
        factory.TokensRepository.AddToken(userId, TokenTypes.EmailVerification, "123456", DateTime.UtcNow.AddMinutes(15));
        var tokenId = factory.TokensRepository.GetAllTokensForUser(userId).First().Id;

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/verify")
        {
            Content = JsonContent.Create(new { code = "123456" })
        };
        req.Headers.Add("Cookie", cookie);
        await client.SendAsync(req);

        // Verify both operations completed
        var user = await factory.UsersRepository.GetByIdAsync(userId);
        user!.EmailVerified.Should().BeTrue("user should be verified");

        var token = factory.TokensRepository.GetToken(tokenId);
        token!.UsedAt.Should().NotBeNull("token should be marked as used");
    }

    private record ErrorResponse(string Error, string Code);
    private record VerifyResponse(bool Verified, string Message);
}

