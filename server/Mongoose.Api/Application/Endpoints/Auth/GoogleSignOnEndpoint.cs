using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Application.Endpoints.Auth;

/// <summary>
/// Google Sign-On Endpoint
/// Implements the OAuth 2.0 authorization code flow against Google's identity provider:
/// - GET /api/v2/auth/google/login    - sets a CSRF state cookie and redirects to Google's consent page
/// - GET /api/v2/auth/google/callback - exchanges the code server-side, signs the user in, and redirects to the app
/// Google hands us a verified email address, so a first-time Google sign-in auto-links
/// to an existing password-based account with the same email instead of always creating
/// a new user (unlike Riot Sign-On, whose accounts have no real email to match on).
/// Gated behind Auth:EnableGoogleSignOn; requires Google OAuth client credentials.
/// Callback is rate limited to 10 requests per 15 minutes per IP.
/// </summary>
public sealed class GoogleSignOnEndpoint : IEndpoint
{
    public string Route { get; }

    internal const string StateCookieName = "mongoose-gso-state";
    private const string ProviderName = "google";
    private const string DefaultAuthorizeEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

    // Rate limiting configuration: 10 callback attempts per 15 minutes per IP
    private const int RateLimitRequests = 10;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(15);

    public GoogleSignOnEndpoint(string basePath)
    {
        Route = basePath + "/auth/google";
    }

    public void Configure(WebApplication app)
    {
        ConfigureLoginEndpoint(app);
        ConfigureCallbackEndpoint(app);
    }

    private void ConfigureLoginEndpoint(WebApplication app)
    {
        app.MapGet(Route + "/login", (
            HttpContext httpContext,
            [FromServices] IConfiguration config,
            [FromServices] ILogger<GoogleSignOnEndpoint> logger
        ) =>
        {
            if (!config.GetValue<bool>("Auth:EnableGoogleSignOn"))
            {
                logger.LogWarning("Google Sign-On login attempt blocked: disabled by configuration");
                return Results.Redirect(ClientUrl(config, "/auth?error=google_signon_disabled"));
            }

            var clientId = config.GetValue<string>("Auth:Google:ClientId") ?? config.GetValue<string>("GSO_CLIENT_ID");
            var redirectUri = config.GetValue<string>("Auth:Google:RedirectUri");
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(redirectUri))
            {
                logger.LogError("Google Sign-On is enabled but client id or redirect URI is not configured");
                return Results.Redirect(ClientUrl(config, "/auth?error=google_signon_failed"));
            }

            // CSRF protection: random state travels via the authorize redirect and is
            // echoed back by Google; the cookie proves the callback belongs to this browser.
            // SameSite=Lax (not Strict) because the callback arrives as a cross-site
            // top-level navigation from accounts.google.com.
            var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            httpContext.Response.Cookies.Append(StateCookieName, state, new CookieOptions
            {
                HttpOnly = true,
                Secure = httpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = StateLifetime,
                Path = "/",
                IsEssential = true
            });

            var authorizeEndpoint = config.GetValue<string>("Auth:Google:AuthorizeEndpoint") ?? DefaultAuthorizeEndpoint;
            var authorizeUrl = authorizeEndpoint
                + "?client_id=" + Uri.EscapeDataString(clientId)
                + "&redirect_uri=" + Uri.EscapeDataString(redirectUri)
                + "&response_type=code"
                + "&scope=" + Uri.EscapeDataString("openid email profile")
                + "&state=" + Uri.EscapeDataString(state);

            return Results.Redirect(authorizeUrl);
        });
    }

    private void ConfigureCallbackEndpoint(WebApplication app)
    {
        app.MapGet(Route + "/callback", async (
            HttpContext httpContext,
            [FromQuery] string? code,
            [FromQuery] string? state,
            [FromQuery] string? error,
            [FromServices] IConfiguration config,
            [FromServices] IGoogleSignOnClient googleSignOnClient,
            [FromServices] IUsersRepository usersRepo,
            [FromServices] IUserIdentityProvidersRepository identityProvidersRepo,
            [FromServices] LoginSyncService loginSyncService,
            [FromServices] IRateLimiter rateLimiter,
            [FromServices] ILogger<GoogleSignOnEndpoint> logger
        ) =>
        {
            try
            {
                if (!config.GetValue<bool>("Auth:EnableGoogleSignOn"))
                {
                    logger.LogWarning("Google Sign-On callback blocked: disabled by configuration");
                    return Results.Redirect(ClientUrl(config, "/auth?error=google_signon_disabled"));
                }

                var rateLimitingEnabled = config.GetValue<bool>("RateLimiting:Enabled", true);
                if (rateLimitingEnabled)
                {
                    var clientIp = ClientIpAddressResolver.GetClientIpAddress(httpContext);
                    var rateLimitResult = await rateLimiter.CheckEndpointAsync(
                        "google-signon-callback",
                        clientIp,
                        null,
                        RateLimitRequests,
                        RateLimitWindow);

                    if (!rateLimitResult.IsAllowed)
                    {
                        logger.LogWarning(
                            "Rate limit exceeded for Google Sign-On callback. IP: {IP}",
                            LogSanitizer.Sanitize(clientIp) ?? "unknown");
                        return Results.Redirect(ClientUrl(config, "/auth?error=google_signon_rate_limited"));
                    }
                }

                // The user declined authorization at Google (or Google reported an error)
                if (!string.IsNullOrWhiteSpace(error))
                {
                    logger.LogInformation("Google Sign-On authorization declined: {Error}", LogSanitizer.Sanitize(error));
                    return Results.Redirect(ClientUrl(config, "/auth?error=google_signon_denied"));
                }

                // CSRF check: the state echoed by Google must match the cookie set at login start
                var expectedState = httpContext.Request.Cookies[StateCookieName];
                httpContext.Response.Cookies.Delete(StateCookieName);
                if (string.IsNullOrWhiteSpace(code)
                    || string.IsNullOrWhiteSpace(state)
                    || string.IsNullOrWhiteSpace(expectedState)
                    || !string.Equals(state, expectedState, StringComparison.Ordinal))
                {
                    logger.LogWarning("Google Sign-On callback rejected: missing code or state mismatch");
                    return Results.Redirect(ClientUrl(config, "/auth?error=google_signon_state"));
                }

                GoogleSignOnIdentity identity;
                try
                {
                    identity = await googleSignOnClient.ExchangeCodeAsync(code, httpContext.RequestAborted);
                }
                catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
                {
                    logger.LogError(ex, "Google Sign-On code exchange failed");
                    return Results.Redirect(ClientUrl(config, "/auth?error=google_signon_failed"));
                }

                var existingUserId = await identityProvidersRepo.GetUserIdByProviderIdentityAsync(ProviderName, identity.GoogleId);
                var user = existingUserId.HasValue ? await usersRepo.GetByIdAsync(existingUserId.Value) : null;
                if (user == null)
                {
                    user = await ResolveOrCreateUserAsync(identity, usersRepo, identityProvidersRepo, logger);
                }

                if (!user.IsActive)
                {
                    logger.LogWarning("Google Sign-On login attempt for inactive user {UserId}", LogSanitizer.Sanitize(user.UserId.ToString()));
                    return Results.Redirect(ClientUrl(config, "/auth?error=account_deactivated"));
                }

                if (string.IsNullOrWhiteSpace(user.SecurityStamp))
                {
                    user.SecurityStamp = Guid.NewGuid().ToString();
                }

                user.LastLoginAt = DateTime.UtcNow;
                await usersRepo.UpsertAsync(user);

                var authProperties = AuthSessionFactory.CreatePersistentSlidingSession();
                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    AuthSessionFactory.CreatePrincipal(user),
                    authProperties
                );

                logger.LogInformation("User {Username} (ID: {UserId}) logged in via Google Sign-On",
                    LogSanitizer.Sanitize(user.Username), LogSanitizer.Sanitize(user.UserId.ToString()));

                // Check linked Riot accounts for new matches, same as password login
                var userId = user.UserId;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await loginSyncService.CheckAccountsOnLoginAsync(userId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Background sync check failed for user {UserId}", LogSanitizer.Sanitize(userId.ToString()));
                    }
                });

                return Results.Redirect(ClientUrl(config, "/app/overview"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in GoogleSignOnEndpoint callback");
                return Results.Redirect(ClientUrl(config, "/auth?error=google_signon_failed"));
            }
        });
    }

    /// <summary>
    /// Resolves the user for a first-time Google Sign-On identity. If a local account
    /// already exists with the same, Google-verified email, the Google identity is
    /// linked to it (auto-link) so the user can sign in either way going forward.
    /// Otherwise a new account is provisioned: no password of its own, email verification
    /// skipped since Google is the identity source of truth for a verified email.
    /// Unverified Google emails never auto-link to an existing account, to avoid a path
    /// where an attacker claims an email they don't control at Google to hijack a local
    /// account — they still get a new account, just not linked to someone else's.
    /// </summary>
    private static async Task<User> ResolveOrCreateUserAsync(
        GoogleSignOnIdentity identity,
        IUsersRepository usersRepo,
        IUserIdentityProvidersRepository identityProvidersRepo,
        ILogger logger)
    {
        if (identity.EmailVerified)
        {
            var existingByEmail = await usersRepo.GetByEmailAsync(identity.Email);
            if (existingByEmail != null)
            {
                existingByEmail.EmailVerified = true;
                await usersRepo.UpsertAsync(existingByEmail);
                await identityProvidersRepo.LinkProviderIdentityAsync(existingByEmail.UserId, ProviderName, identity.GoogleId);

                logger.LogInformation("Auto-linked Google identity to existing user {UserId} by verified email",
                    LogSanitizer.Sanitize(existingByEmail.UserId.ToString()));

                return existingByEmail;
            }
        }

        var username = await GenerateUniqueUsernameAsync(identity, usersRepo);

        var user = new User
        {
            Email = identity.Email,
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
            SecurityStamp = Guid.NewGuid().ToString(),
            EmailVerified = identity.EmailVerified,
            IsActive = true,
            Tier = "free",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.UserId = await usersRepo.UpsertAsync(user);
        await identityProvidersRepo.LinkProviderIdentityAsync(user.UserId, ProviderName, identity.GoogleId);

        logger.LogInformation("Created user {Username} (ID: {UserId}) from Google Sign-On identity",
            LogSanitizer.Sanitize(user.Username),
            LogSanitizer.Sanitize(user.UserId.ToString()));

        return user;
    }

    /// <summary>
    /// Derives a username from the Google display name (matching the register endpoint's
    /// allowed charset), falling back to the email local-part, and appends a suffix until
    /// it is unique.
    /// </summary>
    private static async Task<string> GenerateUniqueUsernameAsync(GoogleSignOnIdentity identity, IUsersRepository usersRepo)
    {
        var source = string.IsNullOrWhiteSpace(identity.Name) ? identity.Email.Split('@')[0] : identity.Name;
        var baseName = new string(source.ToLowerInvariant().Trim()
            .Where(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-')
            .ToArray());

        if (baseName.Length < 3)
        {
            baseName = "google-user";
        }
        else if (baseName.Length > 40)
        {
            baseName = baseName[..40];
        }

        var candidate = baseName;
        while (await usersRepo.UsernameExistsAsync(candidate))
        {
            candidate = $"{baseName}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(3)).ToLowerInvariant()}";
        }

        return candidate;
    }

    /// <summary>
    /// Builds a browser redirect URL into the SPA. Auth:Google:ClientBaseUrl covers
    /// setups where the SPA origin differs from the API (e.g. local dev); when
    /// unset, redirects are relative to the API origin.
    /// </summary>
    private static string ClientUrl(IConfiguration config, string path)
    {
        var clientBaseUrl = config.GetValue<string>("Auth:Google:ClientBaseUrl")?.TrimEnd('/');
        return string.IsNullOrWhiteSpace(clientBaseUrl) ? path : clientBaseUrl + path;
    }
}
