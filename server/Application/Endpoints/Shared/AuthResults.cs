namespace Mongoose.Api.Application.Endpoints.Shared;

/// <summary>
/// Standardized authentication/authorization error responses.
/// Use these instead of Results.Unauthorized() to ensure consistent error codes
/// that the frontend can use to distinguish between different auth failure scenarios.
/// </summary>
public static class AuthResults
{
    /// <summary>
    /// Session has expired. User was previously authenticated but their session is no longer valid.
    /// Frontend should show session-expired UX and prompt re-login.
    /// </summary>
    public static IResult SessionExpired() =>
        Results.Json(
            new { error = "Your session has expired. Please log in again.", code = "SESSION_EXPIRED" },
            statusCode: 401);

    /// <summary>
    /// User is not authenticated (never logged in or cookie missing).
    /// Frontend should redirect to login.
    /// </summary>
    public static IResult NotAuthenticated() =>
        Results.Json(
            new { error = "Authentication required.", code = "NOT_AUTHENTICATED" },
            statusCode: 401);

    /// <summary>
    /// User's session is invalid (e.g., user ID claim missing, user not found in DB, account deactivated).
    /// This is an edge case that shouldn't normally happen. Treat as session expired.
    /// </summary>
    public static IResult InvalidSession() =>
        Results.Json(
            new { error = "Your session is invalid. Please log in again.", code = "SESSION_EXPIRED" },
            statusCode: 401);

    /// <summary>
    /// User is authenticated but not authorized for this resource.
    /// </summary>
    public static IResult Forbidden() =>
        Results.Json(
            new { error = "Access denied.", code = "FORBIDDEN" },
            statusCode: 403);

    /// <summary>
    /// Invalid login credentials (wrong username/password).
    /// This is NOT a session expiry - it's a failed login attempt.
    /// </summary>
    public static IResult InvalidCredentials() =>
        Results.Json(
            new { error = "Invalid username or password", code = "INVALID_CREDENTIALS" },
            statusCode: 401);

    /// <summary>
    /// Account has been deactivated.
    /// </summary>
    public static IResult AccountDeactivated() =>
        Results.Json(
            new { error = "This account has been deactivated", code = "ACCOUNT_DEACTIVATED" },
            statusCode: 401);
}

