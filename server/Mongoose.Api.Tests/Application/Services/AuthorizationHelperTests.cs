using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mongoose.Api.Application.Services;
using Xunit;

namespace Mongoose.Api.Tests;

public class AuthorizationHelperTests
{
    // ─────────────── Helpers ───────────────

    private static HttpContext CreateAuthenticatedContext(string userId, string? username = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
        };
        if (username != null)
            claims.Add(new Claim(ClaimTypes.Name, username));

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext();
        context.User = principal;
        return context;
    }

    private static HttpContext CreateUnauthenticatedContext()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity()); // no auth type = not authenticated
        return context;
    }

    private static ILogger Logger => NullLogger.Instance;

    // ─────────────── ValidateAuthenticatedUser ───────────────

    [Fact]
    public void ValidateAuthenticatedUser_ReturnsNull_WhenUserMatchesRouteId()
    {
        var context = CreateAuthenticatedContext("42");

        var result = AuthorizationHelper.ValidateAuthenticatedUser(context, "42", Logger);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateAuthenticatedUser_Returns401_WhenNotAuthenticated()
    {
        var context = CreateUnauthenticatedContext();

        var result = AuthorizationHelper.ValidateAuthenticatedUser(context, "42", Logger);

        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IStatusCodeHttpResult>();
        ((IStatusCodeHttpResult)result!).StatusCode.Should().Be(401);
    }

    [Fact]
    public void ValidateAuthenticatedUser_Returns400_WhenUserIdIsNotAValidInteger()
    {
        var context = CreateAuthenticatedContext("42");

        var result = AuthorizationHelper.ValidateAuthenticatedUser(context, "not-a-number", Logger);

        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IStatusCodeHttpResult>();
        ((IStatusCodeHttpResult)result!).StatusCode.Should().Be(400);
    }

    [Fact]
    public void ValidateAuthenticatedUser_Returns403_WhenAuthenticatedUserDoesNotMatchRouteId()
    {
        var context = CreateAuthenticatedContext("42");

        var result = AuthorizationHelper.ValidateAuthenticatedUser(context, "99", Logger);

        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IStatusCodeHttpResult>();
        ((IStatusCodeHttpResult)result!).StatusCode.Should().Be(403);
    }

    // ─────────────── GetAuthorizedUser ───────────────

    [Fact]
    public void GetAuthorizedUser_ReturnsCorrectUserIdAndUsername()
    {
        var context = CreateAuthenticatedContext("42", "mongoose_player");

        var user = AuthorizationHelper.GetAuthorizedUser(context);

        user.UserId.Should().Be(42L);
        user.Username.Should().Be("mongoose_player");
    }

    [Fact]
    public void GetAuthorizedUser_ParsesUserIdAsLong()
    {
        var context = CreateAuthenticatedContext("123456789");

        var user = AuthorizationHelper.GetAuthorizedUser(context);

        user.UserId.Should().Be(123456789L);
    }

    [Fact]
    public void GetAuthorizedUser_ReturnsNullUsername_WhenNameClaimAbsent()
    {
        var context = CreateAuthenticatedContext("42"); // no username claim

        var user = AuthorizationHelper.GetAuthorizedUser(context);

        user.Username.Should().BeNull();
    }

    [Fact]
    public void GetAuthorizedUser_ThrowsInvalidOperationException_WhenCalledWithoutValidClaims()
    {
        var context = CreateUnauthenticatedContext();

        var act = () => AuthorizationHelper.GetAuthorizedUser(context);

        act.Should().Throw<InvalidOperationException>();
    }

    // ─────────────── ValidateAndGetUser ───────────────

    [Fact]
    public void ValidateAndGetUser_ReturnsNullErrorAndAuthorizedUser_OnSuccess()
    {
        var context = CreateAuthenticatedContext("42", "player");

        var (error, user) = AuthorizationHelper.ValidateAndGetUser(context, "42", Logger);

        error.Should().BeNull();
        user.Should().NotBeNull();
        user!.UserId.Should().Be(42L);
        user.Username.Should().Be("player");
    }

    [Fact]
    public void ValidateAndGetUser_ReturnsErrorResultAndNullUser_WhenNotAuthenticated()
    {
        var context = CreateUnauthenticatedContext();

        var (error, user) = AuthorizationHelper.ValidateAndGetUser(context, "42", Logger);

        error.Should().NotBeNull();
        user.Should().BeNull();
    }

    // ─────────────── GetAuthenticatedUser ───────────────

    [Fact]
    public void GetAuthenticatedUser_ReturnsNullErrorAndUser_WhenAuthenticated()
    {
        var context = CreateAuthenticatedContext("7", "tester");

        var (error, user) = AuthorizationHelper.GetAuthenticatedUser(context, Logger);

        error.Should().BeNull();
        user.Should().NotBeNull();
        user!.UserId.Should().Be(7L);
        user.Username.Should().Be("tester");
    }

    [Fact]
    public void GetAuthenticatedUser_ReturnsErrorAndNullUser_WhenNotAuthenticated()
    {
        var context = CreateUnauthenticatedContext();

        var (error, user) = AuthorizationHelper.GetAuthenticatedUser(context, Logger);

        error.Should().NotBeNull();
        user.Should().BeNull();
    }
}
