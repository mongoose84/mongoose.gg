using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RiotProxy.Infrastructure;
using RiotProxy.Application;
using RiotProxy.Infrastructure.External.Database;
using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External.Database.Repositories.V2;
using RiotProxy.Infrastructure.External.Riot;
using RiotProxy.Infrastructure.External;
using RiotProxy.Infrastructure.Security;
using RiotProxy.Infrastructure.WebSocket;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Read secrets from configuration/environment (no local secret files required)
Secrets.Initialize(builder.Configuration);

builder.Services.AddSingleton<IRiotApiClient, RiotApiClient>();
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddSingleton<IV2DbConnectionFactory, V2DbConnectionFactory>();

// Email encryption for secure storage - registered via factory to allow test override
builder.Services.AddSingleton<IEmailEncryptor>(sp =>
{
    // Re-read from configuration in case tests have overridden it
    var config = sp.GetRequiredService<IConfiguration>();
    var encryptionKey = config["Security:EmailEncryptionKey"]
        ?? config["EMAIL_ENCRYPTION_KEY"]
        ?? Environment.GetEnvironmentVariable("EMAIL_ENCRYPTION_KEY")
        ?? Secrets.EmailEncryptionKey;

    if (string.IsNullOrWhiteSpace(encryptionKey))
    {
        throw new InvalidOperationException(
            "Email encryption key is not configured. " +
            "Set Security:EmailEncryptionKey in appsettings.json, " +
            "EMAIL_ENCRYPTION_KEY environment variable, " +
            "Generate a key using: AesEmailEncryptor.GenerateKey()");
    }
    return new AesEmailEncryptor(encryptionKey);
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<GamerRepository>();
builder.Services.AddScoped<UserGamerRepository>();
builder.Services.AddScoped<LolMatchRepository>();
builder.Services.AddScoped<LolMatchParticipantRepository>();
builder.Services.AddScoped<SoloStatsRepository>();
builder.Services.AddScoped<DuoStatsRepository>();
builder.Services.AddScoped<TeamStatsRepository>();
// V2 repositories
builder.Services.AddScoped<V2UsersRepository>();
builder.Services.AddScoped<V2RiotAccountsRepository>();
builder.Services.AddScoped<V2MatchesRepository>();
builder.Services.AddScoped<V2ParticipantsRepository>();
builder.Services.AddScoped<V2ParticipantCheckpointsRepository>();
builder.Services.AddScoped<V2ParticipantMetricsRepository>();
builder.Services.AddScoped<V2TeamObjectivesRepository>();
builder.Services.AddScoped<V2ParticipantObjectivesRepository>();
builder.Services.AddScoped<V2TeamMatchMetricsRepository>();
builder.Services.AddScoped<V2DuoMetricsRepository>();
builder.Services.AddScoped<V2SoloStatsRepository>();

// Named HttpClient for Riot API
builder.Services.AddHttpClient("RiotApi", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    // If you keep the Riot API key in Secrets, set the header here
    if (!string.IsNullOrWhiteSpace(Secrets.ApiKey))
        client.DefaultRequestHeaders.Add("X-Riot-Token", Secrets.ApiKey);
});

var enableMatchHistorySync = builder.Configuration.GetValue<bool>("Jobs:EnableMatchHistorySync", true);
if (enableMatchHistorySync)
{
    builder.Services.AddSingleton<MatchHistorySyncJob>();
    builder.Services.AddHostedService(provider => provider.GetRequiredService<MatchHistorySyncJob>());
}

// V2 Match History Sync Job (per-account sync for linked Riot accounts)
var enableV2MatchHistorySync = builder.Configuration.GetValue<bool>("Jobs:EnableV2MatchHistorySync", true);
if (enableV2MatchHistorySync)
{
    builder.Services.AddHostedService<V2MatchHistorySyncJob>();
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

        // APIs should respond with HTTP status codes instead of HTML redirects
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    // Give the policy a name so you can refer to it later
    options.AddPolicy("VueClientPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173", // <-- Vue dev server (client)
                "http://localhost:5174", // <-- Vue dev server (client_v2)
                "http://lol.agileastronaut.com",
                "https://lol.agileastronaut.com",
                "http://www.lol.agileastronaut.com",
                "https://www.lol.agileastronaut.com"
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

var app = builder.Build();

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
        // Reject unauthenticated connections with 4001 (custom close code for auth failure)
        var ws = await context.WebSockets.AcceptWebSocketAsync();
        await ws.CloseAsync(
            (System.Net.WebSockets.WebSocketCloseStatus)4001,
            "Authentication required",
            CancellationToken.None);
        return;
    }

    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    await hub.HandleConnectionAsync(webSocket, userId, context.RequestAborted);
});

// Enable routing and map endpoints
var riotProxyApplication = new RiotProxyApplication(app);
riotProxyApplication.ConfigureEndpoints();

app.Run();

// Expose Program for integration testing
public partial class Program { }