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
/// Riot Sign-On (RSO) Endpoint
/// Implements the OAuth 2.0 authorization code flow against Riot's identity provider:
/// - GET /api/v2/auth/riot/login    - sets a CSRF state cookie and redirects to Riot's authorize page
/// - GET /api/v2/auth/riot/callback - exchanges the code server-side, signs the user in, and redirects to the app
/// Because Riot hands us the authenticated PUUID directly, the Riot account is linked
/// automatically — no manual account linking step is required for RSO users.
/// Gated behind Auth:EnableRiotSignOn; requires RSO client credentials issued by Riot.
/// Callback is rate limited to 10 requests per 15 minutes per IP.
/// </summary>
public sealed class RiotSignOnEndpoint : IEndpoint
{
    public string Route { get; }

    internal const string StateCookieName = "mongoose-rso-state";
    private const string ProviderName = "riot";
    private const string DefaultAuthorizeEndpoint = "https://auth.riotgames.com/authorize";
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

    // Rate limiting configuration: 10 callback attempts per 15 minutes per IP
    private const int RateLimitRequests = 10;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(15);

    public RiotSignOnEndpoint(string basePath)
    {
        Route = basePath + "/auth/riot";
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
            [FromServices] ILogger<RiotSignOnEndpoint> logger
        ) =>
        {
            if (!config.GetValue<bool>("Auth:EnableRiotSignOn"))
            {
                logger.LogWarning("Riot Sign-On login attempt blocked: disabled by configuration");
                return Results.Redirect(ClientUrl(config, "/auth?error=riot_signon_disabled"));
            }

            var clientId = config.GetValue<string>("Auth:Riot:ClientId") ?? config.GetValue<string>("RSO_CLIENT_ID");
            var redirectUri = config.GetValue<string>("Auth:Riot:RedirectUri");
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(redirectUri))
            {
                logger.LogError("Riot Sign-On is enabled but client id or redirect URI is not configured");
                return Results.Redirect(ClientUrl(config, "/auth?error=riot_signon_failed"));
            }

            // CSRF protection: random state travels via the authorize redirect and is
            // echoed back by Riot; the cookie proves the callback belongs to this browser.
            // SameSite=Lax (not Strict) because the callback arrives as a cross-site
            // top-level navigation from auth.riotgames.com.
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

            var authorizeEndpoint = config.GetValue<string>("Auth:Riot:AuthorizeEndpoint") ?? DefaultAuthorizeEndpoint;
            var authorizeUrl = authorizeEndpoint
                + "?client_id=" + Uri.EscapeDataString(clientId)
                + "&redirect_uri=" + Uri.EscapeDataString(redirectUri)
                + "&response_type=code"
                + "&scope=" + Uri.EscapeDataString("openid cpid")
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
            [FromServices] IRiotSignOnClient riotSignOnClient,
            [FromServices] IUsersRepository usersRepo,
            [FromServices] IUserIdentityProvidersRepository identityProvidersRepo,
            [FromServices] IRiotAccountsRepository riotAccountsRepo,
            [FromServices] IUserRiotAccountsRepository userRiotAccountsRepo,
            [FromServices] LoginSyncService loginSyncService,
            [FromServices] IRateLimiter rateLimiter,
            [FromServices] ILogger<RiotSignOnEndpoint> logger
        ) =>
        {
            try
            {
                if (!config.GetValue<bool>("Auth:EnableRiotSignOn"))
                {
                    logger.LogWarning("Riot Sign-On callback blocked: disabled by configuration");
                    return Results.Redirect(ClientUrl(config, "/auth?error=riot_signon_disabled"));
                }

                var rateLimitingEnabled = config.GetValue<bool>("RateLimiting:Enabled", true);
                if (rateLimitingEnabled)
                {
                    var clientIp = ClientIpAddressResolver.GetClientIpAddress(httpContext);
                    var rateLimitResult = await rateLimiter.CheckEndpointAsync(
                        "riot-signon-callback",
                        clientIp,
                        null,
                        RateLimitRequests,
                        RateLimitWindow);

                    if (!rateLimitResult.IsAllowed)
                    {
                        logger.LogWarning(
                            "Rate limit exceeded for Riot Sign-On callback. IP: {IP}",
                            LogSanitizer.Sanitize(clientIp) ?? "unknown");
                        return Results.Redirect(ClientUrl(config, "/auth?error=riot_signon_rate_limited"));
                    }
                }

                // The user declined authorization at Riot (or Riot reported an error)
                if (!string.IsNullOrWhiteSpace(error))
                {
                    logger.LogInformation("Riot Sign-On authorization declined: {Error}", LogSanitizer.Sanitize(error));
                    return Results.Redirect(ClientUrl(config, "/auth?error=riot_signon_denied"));
                }

                // CSRF check: the state echoed by Riot must match the cookie set at login start
                var expectedState = httpContext.Request.Cookies[StateCookieName];
                httpContext.Response.Cookies.Delete(StateCookieName);
                if (string.IsNullOrWhiteSpace(code)
                    || string.IsNullOrWhiteSpace(state)
                    || string.IsNullOrWhiteSpace(expectedState)
                    || !string.Equals(state, expectedState, StringComparison.Ordinal))
                {
                    logger.LogWarning("Riot Sign-On callback rejected: missing code or state mismatch");
                    return Results.Redirect(ClientUrl(config, "/auth?error=riot_signon_state"));
                }

                RiotSignOnIdentity identity;
                try
                {
                    identity = await riotSignOnClient.ExchangeCodeAsync(code, httpContext.RequestAborted);
                }
                catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
                {
                    logger.LogError(ex, "Riot Sign-On code exchange failed");
                    return Results.Redirect(ClientUrl(config, "/auth?error=riot_signon_failed"));
                }

                var existingUserId = await identityProvidersRepo.GetUserIdByProviderIdentityAsync(ProviderName, identity.Puuid);
                var user = existingUserId.HasValue ? await usersRepo.GetByIdAsync(existingUserId.Value) : null;
                if (user == null)
                {
                    user = await CreateUserForRiotIdentityAsync(identity, usersRepo, identityProvidersRepo, logger);
                }
                else if (!user.IsActive)
                {
                    logger.LogWarning("Riot Sign-On login attempt for inactive user {UserId}", LogSanitizer.Sanitize(user.UserId.ToString()));
                    return Results.Redirect(ClientUrl(config, "/auth?error=account_deactivated"));
                }

                await EnsureRiotAccountLinkedAsync(user, identity, config, riotAccountsRepo, userRiotAccountsRepo, logger);

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

                logger.LogInformation("User {Username} (ID: {UserId}) logged in via Riot Sign-On",
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
                logger.LogError(ex, "Error in RiotSignOnEndpoint callback");
                return Results.Redirect(ClientUrl(config, "/auth?error=riot_signon_failed"));
            }
        });
    }

    /// <summary>
    /// Provisions a new user for a first-time Riot Sign-On login. RSO users have no
    /// email or password of their own: the email is a synthetic unique placeholder,
    /// the password hash is random (unusable), and email verification is skipped.
    /// </summary>
    private static async Task<User> CreateUserForRiotIdentityAsync(
        RiotSignOnIdentity identity,
        IUsersRepository usersRepo,
        IUserIdentityProvidersRepository identityProvidersRepo,
        ILogger logger)
    {
        var username = await GenerateUniqueUsernameAsync(identity, usersRepo);

        var user = new User
        {
            Email = $"{identity.Puuid.ToLowerInvariant()}@riot-signon.invalid",
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
            SecurityStamp = Guid.NewGuid().ToString(),
            EmailVerified = true,
            IsActive = true,
            Tier = "free",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.UserId = await usersRepo.UpsertAsync(user);
        await identityProvidersRepo.LinkProviderIdentityAsync(user.UserId, ProviderName, identity.Puuid);

        logger.LogInformation("Created user {Username} (ID: {UserId}) from Riot Sign-On identity {Puuid}",
            LogSanitizer.Sanitize(user.Username),
            LogSanitizer.Sanitize(user.UserId.ToString()),
            LogSanitizer.HashForLog(identity.Puuid));

        return user;
    }

    /// <summary>
    /// Derives a username from the Riot game name (matching the register endpoint's
    /// allowed charset) and appends a suffix until it is unique.
    /// </summary>
    private static async Task<string> GenerateUniqueUsernameAsync(RiotSignOnIdentity identity, IUsersRepository usersRepo)
    {
        var baseName = new string(identity.GameName.ToLowerInvariant().Trim()
            .Where(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-')
            .ToArray());

        if (baseName.Length < 3)
        {
            baseName = "riot-player";
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
    /// Ensures the RSO identity's Riot account exists and is linked to the user.
    /// Identity is handed to us by Riot, so no manual linking or Riot ID lookup is
    /// needed. New accounts start with sync_status 'pending'; the background sync
    /// job enriches profile and rank data.
    /// </summary>
    private static async Task EnsureRiotAccountLinkedAsync(
        User user,
        RiotSignOnIdentity identity,
        IConfiguration config,
        IRiotAccountsRepository riotAccountsRepo,
        IUserRiotAccountsRepository userRiotAccountsRepo,
        ILogger logger)
    {
        var existingAccount = await riotAccountsRepo.GetByPuuidAsync(identity.Puuid);
        if (existingAccount == null)
        {
            var region = identity.Region ?? config.GetValue<string>("Auth:Riot:DefaultRegion") ?? "euw1";
            var gameName = string.IsNullOrWhiteSpace(identity.GameName) ? "Unknown" : identity.GameName;
            var tagLine = string.IsNullOrWhiteSpace(identity.TagLine) ? "???" : identity.TagLine;

            await riotAccountsRepo.UpsertAsync(new RiotAccount
            {
                Puuid = identity.Puuid,
                GameName = gameName,
                TagLine = tagLine,
                SummonerName = $"{gameName}#{tagLine}",
                Region = region,
                SyncStatus = "pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        var alreadyLinked = await userRiotAccountsRepo.IsLinkedAsync(user.UserId, identity.Puuid);
        if (!alreadyLinked)
        {
            var existingLinks = await userRiotAccountsRepo.GetByUserIdAsync(user.UserId);
            await userRiotAccountsRepo.LinkAsync(user.UserId, identity.Puuid, isPrimary: existingLinks.Count == 0);

            logger.LogInformation("Auto-linked Riot account {Puuid} to user {UserId} via Riot Sign-On",
                LogSanitizer.HashForLog(identity.Puuid), LogSanitizer.Sanitize(user.UserId.ToString()));
        }
    }

    /// <summary>
    /// Builds a browser redirect URL into the SPA. Auth:Riot:ClientBaseUrl covers
    /// setups where the SPA origin differs from the API (e.g. local dev); when
    /// unset, redirects are relative to the API origin.
    /// </summary>
    private static string ClientUrl(IConfiguration config, string path)
    {
        var clientBaseUrl = config.GetValue<string>("Auth:Riot:ClientBaseUrl")?.TrimEnd('/');
        return string.IsNullOrWhiteSpace(clientBaseUrl) ? path : clientBaseUrl + path;
    }
}
