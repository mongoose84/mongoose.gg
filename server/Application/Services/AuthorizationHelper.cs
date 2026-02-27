using System.Security.Claims;
using Mongoose.Api.Application.Endpoints.Shared;

namespace Mongoose.Api.Application.Services;

/// <summary>
/// Reusable helper for authentication and authorization checks in endpoints.
/// Eliminates code duplication and ensures consistent security enforcement.
/// </summary>
public sealed class AuthorizationHelper
{
    /// <summary>
    /// Represents an authenticated and authorized user.
    /// </summary>
    /// <param name="UserId">The validated user ID</param>
    /// <param name="Username">The username from claims (optional)</param>
    public record AuthorizedUser(long UserId, string? Username);

    /// <summary>
    /// Validates that the current HTTP request is authenticated and that the authenticated user
    /// matches the userId parameter from the route.
    /// </summary>
    /// <param name="httpContext">The HTTP context containing user claims</param>
    /// <param name="userIdParam">The userId from the route parameter</param>
    /// <param name="logger">Logger for audit trail</param>
    /// <returns>
    /// An IResult error response if validation fails (401/403/400), or null if validation succeeds.
    /// When null is returned, call GetAuthorizedUser to retrieve the validated user information.
    /// </returns>
    public static IResult? ValidateAuthenticatedUser(
        HttpContext httpContext,
        string? userIdParam,
        ILogger logger)
    {
        // Check authentication
        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            return AuthResults.NotAuthenticated();
        }

        // Validate userId format
        if (!int.TryParse(userIdParam, out var userIdInt))
        {
            logger.LogWarning("Invalid userId format: {UserId}",
                LogSanitizer.Sanitize(userIdParam));
            return Results.BadRequest(new { error = "Invalid userId format" });
        }

        // Check authorization - user can only access their own data
        var authenticatedUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(authenticatedUserId) || authenticatedUserId != userIdInt.ToString())
        {
            logger.LogWarning("User {AuthUserId} attempted to access data for user {UserId}",
                authenticatedUserId ?? "unknown", userIdInt);
            return Results.Forbid();
        }

        return null; // Validation succeeded
    }

    /// <summary>
    /// Retrieves the authenticated user information from the HTTP context.
    /// Should only be called after ValidateAuthenticatedUser returns null (success).
    /// </summary>
    /// <param name="httpContext">The HTTP context containing user claims</param>
    /// <returns>The authorized user information</returns>
    /// <exception cref="InvalidOperationException">Thrown if called without prior validation</exception>
    public static AuthorizedUser GetAuthorizedUser(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException(
                "GetAuthorizedUser called without valid authentication. " +
                "Call ValidateAuthenticatedUser first.");
        }

        var username = httpContext.User.FindFirst(ClaimTypes.Name)?.Value;

        return new AuthorizedUser(userId, username);
    }

    /// <summary>
    /// Combined validation and retrieval. Validates the request and returns the authorized user
    /// or an error result.
    /// </summary>
    /// <param name="httpContext">The HTTP context containing user claims</param>
    /// <param name="userIdParam">The userId from the route parameter</param>
    /// <param name="logger">Logger for audit trail</param>
    /// <returns>
    /// A tuple containing either (null, AuthorizedUser) on success or (IResult, null) on failure.
    /// </returns>
    public static (IResult? ErrorResult, AuthorizedUser? User) ValidateAndGetUser(
        HttpContext httpContext,
        string? userIdParam,
        ILogger logger)
    {
        var errorResult = ValidateAuthenticatedUser(httpContext, userIdParam, logger);
        if (errorResult != null)
        {
            return (errorResult, null);
        }

        var user = GetAuthorizedUser(httpContext);
        return (null, user);
    }
}
