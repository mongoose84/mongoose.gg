using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Mongoose.Api.Core.Entities;
using Xunit;

namespace Mongoose.Api.Tests;

public class AccountSecurityEndpointsTests
{
    private static async Task<string> LoginAndGetCookieAsync(TestWebApplicationFactory factory, string username = "tester", string password = "test-password")
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var loginResponse = await client.PostAsJsonAsync("/api/v2/auth/login", new { username, password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        return cookies!.First().Split(';', 2)[0];
    }

    [Fact]
    public async Task ForgotPassword_returns_200_for_unknown_email_to_prevent_enumeration()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/forgot-password", new { email = "unknown@example.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.EmailService.SentPasswordResetEmails.Should().BeEmpty();
    }

    [Fact]
    public async Task ForgotPassword_creates_token_and_sends_email_for_existing_user()
    {
        using var factory = new TestWebApplicationFactory();
        factory.EmailService.Clear();
        var user = await factory.UsersRepository.GetByEmailAsync("tester@test.com");
        user.Should().NotBeNull();

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.PostAsJsonAsync("/api/v2/auth/forgot-password", new { email = "tester@test.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.EmailService.SentPasswordResetEmails.Should().HaveCount(1);
        factory.EmailService.SentPasswordResetEmails[0].ToEmail.Should().Be("tester@test.com");

        var tokens = factory.TokensRepository.GetAllTokensForUser(user!.UserId)
            .Where(t => t.TokenType == TokenTypes.PasswordReset)
            .ToList();
        tokens.Should().NotBeEmpty();
        tokens.Should().Contain(t => t.UsedAt == null);
    }

    [Fact]
    public async Task ResetPassword_rejects_invalid_code_format()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/reset-password", new
        {
            email = "tester@test.com",
            code = "abc",
            newPassword = "new-password-123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("code").GetString().Should().Be("INVALID_CODE");
    }

    [Fact]
    public async Task ResetPassword_updates_password_and_marks_token_used_when_code_is_valid()
    {
        using var factory = new TestWebApplicationFactory();
        var user = await factory.UsersRepository.GetByEmailAsync("tester@test.com");
        user.Should().NotBeNull();

        factory.TokensRepository.AddToken(user!.UserId, TokenTypes.PasswordReset, "123456", DateTime.UtcNow.AddMinutes(15));
        var token = factory.TokensRepository.GetAllTokensForUser(user.UserId)
            .First(t => t.TokenType == TokenTypes.PasswordReset && t.Code == "123456");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var resetResponse = await client.PostAsJsonAsync("/api/v2/auth/reset-password", new
        {
            email = "tester@test.com",
            code = "123456",
            newPassword = "new-password-123"
        });

        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedToken = factory.TokensRepository.GetToken(token.Id);
        updatedToken.Should().NotBeNull();
        updatedToken!.UsedAt.Should().NotBeNull();

        var oldPasswordLogin = await client.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        oldPasswordLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newPasswordLogin = await client.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "new-password-123" });
        newPasswordLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_requires_authentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/v2/auth/change-password", new
        {
            currentPassword = "test-password",
            newPassword = "new-password-123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_rejects_invalid_current_password()
    {
        using var factory = new TestWebApplicationFactory();
        var cookie = await LoginAndGetCookieAsync(factory);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/change-password")
        {
            Content = JsonContent.Create(new
            {
                currentPassword = "wrong-password",
                newPassword = "new-password-123"
            })
        };
        request.Headers.Add("Cookie", cookie);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("code").GetString().Should().Be("INVALID_PASSWORD");
    }

    [Fact]
    public async Task ChangePassword_updates_password_and_invalidates_old_credentials()
    {
        using var factory = new TestWebApplicationFactory();
        var cookie = await LoginAndGetCookieAsync(factory);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/auth/change-password")
        {
            Content = JsonContent.Create(new
            {
                currentPassword = "test-password",
                newPassword = "new-password-123"
            })
        };
        request.Headers.Add("Cookie", cookie);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var oldPasswordLogin = await client.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        oldPasswordLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newPasswordLogin = await client.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "new-password-123" });
        newPasswordLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteAccount_requires_authentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.DeleteAsync("/api/v2/auth/account");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAccount_rejects_invalid_password()
    {
        using var factory = new TestWebApplicationFactory();
        var cookie = await LoginAndGetCookieAsync(factory);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v2/auth/account")
        {
            Content = JsonContent.Create(new { password = "wrong-password" })
        };
        request.Headers.Add("Cookie", cookie);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("code").GetString().Should().Be("INVALID_PASSWORD");
    }

    [Fact]
    public async Task DeleteAccount_removes_user_and_prevents_future_login()
    {
        using var factory = new TestWebApplicationFactory();
        var cookie = await LoginAndGetCookieAsync(factory);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/v2/auth/account")
        {
            Content = JsonContent.Create(new { password = "test-password" })
        };
        deleteRequest.Headers.Add("Cookie", cookie);

        var deleteResponse = await client.SendAsync(deleteRequest);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await client.PostAsJsonAsync("/api/v2/auth/login", new { username = "tester", password = "test-password" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var deletedUser = await factory.UsersRepository.GetByUsernameAsync("tester");
        deletedUser.Should().BeNull();
    }
}
