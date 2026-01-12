using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace RiotProxy.Application.Endpoints.Auth;

/// <summary>
/// v2 Logout Endpoint
/// Clears the session cookie and signs out the user.
/// </summary>
public sealed class LogoutEndpoint : IEndpoint
{
    public string Route { get; }

    public LogoutEndpoint(string basePath)
    {
        Route = basePath + "/auth/logout";
    }

    public void Configure(WebApplication app)
    {
        app.MapPost(Route, async (
            HttpContext httpContext,
            [FromServices] ILogger<LogoutEndpoint> logger
        ) =>
        {
            try
            {
                var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                
                if (!string.IsNullOrEmpty(userId))
                    logger.LogInformation("User {UserId} logged out successfully", userId);
                
                return Results.Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in LogoutEndpoint");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }
}
