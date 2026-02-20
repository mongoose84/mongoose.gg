using Microsoft.AspNetCore.Authentication.Cookies;
using Mongoose.Api.Application;
using Mongoose.Api.Application.Interfaces;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure;
using Mongoose.Api.Infrastructure.Database;
using Mongoose.Api.Infrastructure.Database.Repositories;
using Mongoose.Api.Infrastructure.Email;
using Mongoose.Api.Infrastructure.Jobs;
using Mongoose.Api.Infrastructure.Middleware;
using Mongoose.Api.Infrastructure.Riot;
using Mongoose.Api.Infrastructure.Security;
using Mongoose.Api.Infrastructure.Serialization;
using Mongoose.Api.Infrastructure.RateLimiting;
using Mongoose.Api.Infrastructure.WebSocket;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Read secrets from configuration/environment (no local secret files required)
Secrets.Initialize(builder.Configuration);


builder.Services.AddSingleton<IRiotApiClient, RiotApiClient>();
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

// Email encryption for secure storage - registered via factory to allow test override
builder.Services.AddSingleton<IEncryptor>(sp =>
{
    // Re-read from configuration in case tests have overridden it
    var config = sp.GetRequiredService<IConfiguration>();
    var encryptionSecret = config["Security:EncryptionSecret"]
        ?? config["ENCRYPTION_SECRET"]
        ?? Environment.GetEnvironmentVariable("ENCRYPTION_SECRET")
        ?? Secrets.EncryptionSecret;

    if (string.IsNullOrWhiteSpace(encryptionSecret))
    {
        throw new InvalidOperationException("Encryption secret is not configured. ");
    }
    return new AesEncryptor(encryptionSecret);
});

// repositories
builder.Services.AddScoped<UsersRepository>();
builder.Services.AddScoped<IUsersRepository>(sp => sp.GetRequiredService<UsersRepository>());
builder.Services.AddScoped<RiotAccountsRepository>();
builder.Services.AddScoped<IUserRiotAccountsRepository, UserRiotAccountsRepository>();
builder.Services.AddScoped<MatchesRepository>();
builder.Services.AddScoped<IMatchesRepository>(sp => sp.GetRequiredService<MatchesRepository>());
builder.Services.AddScoped<ParticipantsRepository>();
builder.Services.AddScoped<ParticipantCheckpointsRepository>();
builder.Services.AddScoped<ParticipantMetricsRepository>();
builder.Services.AddScoped<TeamObjectivesRepository>();
builder.Services.AddScoped<ParticipantObjectivesRepository>();
builder.Services.AddScoped<IParticipantDeathEventsRepository, ParticipantDeathEventsRepository>();
builder.Services.AddScoped<IDeathPositionsRepository, DeathPositionsRepository>();
builder.Services.AddScoped<TeamMatchMetricsRepository>();
builder.Services.AddScoped<TeamRoleResponsibilitiesRepository>();
builder.Services.AddScoped<DuoMetricsRepository>();
builder.Services.AddScoped<ISoloPerformanceRepository, SoloPerformanceRepository>();
builder.Services.AddScoped<IChampionSelectRepository, ChampionSelectRepository>();
builder.Services.AddScoped<ITrendRepository, TrendRepository>();
builder.Services.AddScoped<IMatchupRepository, MatchupRepository>();
builder.Services.AddScoped<OverviewStatsRepository>();
builder.Services.AddScoped<SeasonsRepository>();
builder.Services.AddScoped<AnalyticsEventsRepository>();
builder.Services.AddScoped<VerificationTokensRepository>();

// Application services
builder.Services.AddScoped<LoginSyncService>();

// Query filter builder for centralized SQL filter generation
builder.Services.AddScoped<IQueryFilterBuilder, QueryFilterBuilder>();

// Email service for verification emails
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();

// GitHub service for feedback integration (uses typed HttpClient)
builder.Services.AddHttpClient<IGitHubService, Mongoose.Api.Infrastructure.GitHub.GitHubService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Rate limiter for endpoint protection (uses distributed cache)
builder.Services.AddSingleton<IRateLimiter, EndpointRateLimiter>();

// Named HttpClient for Riot API
builder.Services.AddHttpClient("RiotApi", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    // If you keep the Riot API key in Secrets, set the header here
    if (!string.IsNullOrWhiteSpace(Secrets.ApiKey))
        client.DefaultRequestHeaders.Add("X-Riot-Token", Secrets.ApiKey);
});

// Match History Sync Job (per-account sync for linked Riot accounts)
var enableMatchHistorySync = builder.Configuration.GetValue<bool>("Jobs:EnableMatchHistorySync", true);
if (enableMatchHistorySync)
{
    builder.Services.AddHostedService<MatchHistorySyncJob>();
}

// Match Cleanup Job (deletes matches older than retention period)
var enableMatchCleanup = builder.Configuration.GetValue<bool>("Jobs:EnableMatchCleanup", true);
if (enableMatchCleanup)
{
    builder.Services.AddHostedService<MatchCleanupJob>();
}

// WebSocket hub for sync progress (singleton - shared across all connections)
builder.Services.AddSingleton<SyncProgressHub>();
builder.Services.AddSingleton<ISyncProgressBroadcaster>(sp => sp.GetRequiredService<SyncProgressHub>());

// Add distributed cache for session storage (in-memory for dev, Redis for prod)
builder.Services.AddDistributedMemoryCache();

// Add session support
var sessionTimeoutMinutes = builder.Configuration.GetValue<int>("Auth:SessionTimeout", 30);
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(sessionTimeoutMinutes);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // set to SameAsRequest for local dev
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Add authentication (cookie-based)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // set to SameAsRequest for local dev
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(sessionTimeoutMinutes);
        options.SlidingExpiration = true;
        var cookieName = builder.Configuration.GetValue<string>("Auth:CookieName");
        if (!string.IsNullOrWhiteSpace(cookieName))
        {
            options.Cookie.Name = cookieName;
        }

        // APIs should respond with HTTP status codes and JSON instead of HTML redirects
        options.Events.OnRedirectToLogin = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            // Note: We cannot reliably distinguish between "session expired" and "never logged in" on the backend.
            // When IsPersistent=true with ExpiresUtc (as used in LoginEndpoint), the browser sets a persistent
            // cookie that expires at ExpiresUtc. When the cookie expires, the browser doesn't send it at all,
            // making it indistinguishable from "never logged in".
            //
            // The frontend handles this by tracking `wasAuthenticated` state - if the user was ever authenticated
            // in the current browser session, a 401 will show the "session expired" banner. Both SESSION_EXPIRED
            // and NOT_AUTHENTICATED codes trigger this behavior on the frontend.
            //
            // We still check for the cookie presence as a best-effort attempt, which can work in some edge cases
            // (e.g., if the server-side ticket expires before the browser cookie).
            var authCookieName = context.Options.Cookie.Name ?? ".AspNetCore.Cookies";
            var hadAuthCookie = context.Request.Cookies.ContainsKey(authCookieName);

            var errorCode = hadAuthCookie ? "SESSION_EXPIRED" : "NOT_AUTHENTICATED";
            var errorMessage = hadAuthCookie
                ? "Your session has expired. Please log in again."
                : "Authentication required.";

            var json = System.Text.Json.JsonSerializer.Serialize(new { error = errorMessage, code = errorCode });
            await context.Response.WriteAsync(json);
        };

        options.Events.OnRedirectToAccessDenied = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            var json = System.Text.Json.JsonSerializer.Serialize(new { error = "Access denied.", code = "FORBIDDEN" });
            await context.Response.WriteAsync(json);
        };

        // Security stamp validation — rejects cookies whose security_stamp
        // no longer matches the database (e.g. after a password change).
        options.Events.OnValidatePrincipal = async context =>
        {
            var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var stampClaim = context.Principal?.FindFirst("security_stamp")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                context.RejectPrincipal();
                return;
            }

            // If no stamp claim (pre-migration cookie), reject so user re-logs and gets a stamp
            if (string.IsNullOrEmpty(stampClaim))
            {
                context.RejectPrincipal();
                return;
            }

            var usersRepo = context.HttpContext.RequestServices.GetRequiredService<UsersRepository>();
            var currentStamp = await usersRepo.GetSecurityStampAsync(userId);

            if (currentStamp == null || !string.Equals(stampClaim, currentStamp, StringComparison.Ordinal))
            {
                context.RejectPrincipal();
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    // Give the policy a name so you can refer to it later
    options.AddPolicy("VueClientPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173", // Vue dev server (default port)
                "http://localhost:5174", // Vue dev server (alternate port)
                "http://localhost:5175", // Vue dev server (alternate port)
                "https://mongoose.gg",
                "https://www.mongoose.gg",
                "https://beta.mongoose.gg"
               )
              .AllowAnyHeader()                      // allow all custom headers (Content-Type, Authorization, etc.)
              .AllowAnyMethod()                      // GET, POST, PUT, DELETE, OPTIONS…
              .AllowCredentials();                   // if you need cookies / Authorization header

        // If you want to allow *any* origin (only for development!), use:
        // policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// Reduce HttpClient request logging noise (toggle via appsettings or here)
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient.RiotApi.LogicalHandler", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient.RiotApi.ClientHandler", LogLevel.Warning);

// In development, print ILogger messages to console (and debug)
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
}

// Configure JSON serialization to ensure DateTime values are serialized in UTC format (ISO 8601 with Z suffix)
// This ensures consistent timezone handling between backend and frontend
builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Use custom DateTime converters that ensure UTC format with Z suffix
    options.SerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
    options.SerializerOptions.Converters.Add(new UtcNullableDateTimeJsonConverter());
});

var app = builder.Build();

// Use custom JSON exception middleware globally
app.UseMiddleware<JsonExceptionMiddleware>();

// Apply the CORS policy globally
app.UseCors("VueClientPolicy");

// Enable WebSocket support
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});

// Session middleware (must come before routing)
app.UseSession();

// AuthN/Z middleware
app.UseAuthentication();
app.UseAuthorization();

// WebSocket endpoint for sync progress at /ws/sync
app.Map("/ws/sync", async (HttpContext context, SyncProgressHub hub) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    // Authenticate using session cookie (same as HTTP endpoints)
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
    if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
    {
        // Reject unauthenticated connections with standard close code 1008 (Policy Violation)
        var ws = await context.WebSockets.AcceptWebSocketAsync();
        await ws.CloseAsync(
            System.Net.WebSockets.WebSocketCloseStatus.PolicyViolation,
            "Authentication required",
            CancellationToken.None);
        return;
    }

    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    await hub.HandleConnectionAsync(webSocket, userId, context.RequestAborted);
});

// Enable routing and map endpoints
var mongooseApiApplication = new MongooseApiApplication(app);
mongooseApiApplication.ConfigureEndpoints();

app.Run();

// Expose Program for integration testing
public partial class Program { }
