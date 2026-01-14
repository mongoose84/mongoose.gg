using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.External.Domain.Entities;
using RiotProxy.Infrastructure.External.Database.Repositories;
using RiotProxy.Infrastructure.External.Riot;

namespace RiotProxy.Application.Endpoints.Auth;

/// <summary>
/// v2 Riot Accounts Endpoint
/// Provides all operations on linked Riot accounts:
/// - POST /api/v2/users/me/riot-accounts - Link a new Riot account
/// - DELETE /api/v2/users/me/riot-accounts/{puuid} - Unlink a Riot account
/// - POST /api/v2/users/me/riot-accounts/{puuid}/sync - Trigger a sync
/// - GET /api/v2/users/me/riot-accounts/{puuid}/sync-status - Get sync status
/// </summary>
public sealed class RiotAccountsEndpoint : IEndpoint
{
    public string Route { get; }

    // Valid regions for Riot accounts
    private static readonly string[] ValidRegions = ["na1", "euw1", "eun1", "kr", "jp1", "br1", "la1", "la2", "oc1", "tr1", "ru", "ph2", "sg2", "th2", "tw2", "vn2"];

    // Regex for game name: allows Unicode letters/numbers, spaces, underscores, hyphens
    // Excludes control characters and common injection characters
    private static readonly Regex GameNameRegex = new(@"^[\p{L}\p{N}\s_\-]+$", RegexOptions.Compiled);

    // Regex for tag line: alphanumeric only (more restrictive per Riot's format)
    private static readonly Regex TagLineRegex = new(@"^[a-zA-Z0-9]+$", RegexOptions.Compiled);

    public RiotAccountsEndpoint(string basePath)
    {
        Route = basePath + "/users/me/riot-accounts";
    }

    public void Configure(WebApplication app)
    {
        ConfigureLinkEndpoint(app);
        ConfigureDeleteEndpoint(app);
        ConfigureSyncEndpoint(app);
        ConfigureSyncStatusEndpoint(app);
    }

    private void ConfigureLinkEndpoint(WebApplication app)
    {
        app.MapPost(Route, [Authorize] async (
            HttpContext httpContext,
            [FromBody] LinkRiotAccountRequest request,
            [FromServices] UsersRepository usersRepo,
            [FromServices] RiotAccountsRepository riotAccountsRepo,
            [FromServices] IRiotApiClient riotApiClient,
            [FromServices] ILogger<RiotAccountsEndpoint> logger
        ) =>
        {
            try
            {
                var userId = GetUserId(httpContext);
                if (userId == null) return Results.Unauthorized();

                // Validate request
                if (string.IsNullOrWhiteSpace(request.GameName))
                {
                    return Results.BadRequest(new { error = "Game name is required", code = "GAME_NAME_REQUIRED" });
                }

                if (string.IsNullOrWhiteSpace(request.TagLine))
                {
                    return Results.BadRequest(new { error = "Tag line is required", code = "TAG_LINE_REQUIRED" });
                }

                // Validate game name length (max 100 chars per schema)
                if (request.GameName.Length > 100)
                {
                    return Results.BadRequest(new { error = "Game name must not exceed 100 characters", code = "GAME_NAME_TOO_LONG" });
                }

                // Validate tag line length (max 10 chars per schema)
                if (request.TagLine.Length > 10)
                {
                    return Results.BadRequest(new { error = "Tag line must not exceed 10 characters", code = "TAG_LINE_TOO_LONG" });
                }

                // Validate game name format
                if (!GameNameRegex.IsMatch(request.GameName))
                {
                    return Results.BadRequest(new { error = "Game name contains invalid characters", code = "GAME_NAME_INVALID_CHARS" });
                }

                // Validate tag line format: alphanumeric only
                if (!TagLineRegex.IsMatch(request.TagLine))
                {
                    return Results.BadRequest(new { error = "Tag line must contain only letters and numbers", code = "TAG_LINE_INVALID_CHARS" });
                }

                if (string.IsNullOrWhiteSpace(request.Region))
                {
                    return Results.BadRequest(new { error = "Region is required", code = "REGION_REQUIRED" });
                }

                // Validate region
                if (!ValidRegions.Contains(request.Region.ToLowerInvariant()))
                {
                    return Results.BadRequest(new { error = $"Invalid region. Valid regions: {string.Join(", ", ValidRegions)}", code = "INVALID_REGION" });
                }

                // Verify user exists and is active
                var user = await usersRepo.GetByIdAsync(userId.Value);
                if (user == null || !user.IsActive)
                {
                    return Results.Unauthorized();
                }

                // Lookup PUUID from Riot API
                string puuid;
                try
                {
                    puuid = await riotApiClient.GetPuuIdAsync(request.GameName, request.TagLine);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    logger.LogWarning("Riot account not found: {GameName}#{TagLine}", request.GameName, request.TagLine);
                    return Results.NotFound(new { error = "Riot account not found", code = "RIOT_ACCOUNT_NOT_FOUND" });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error looking up Riot account: {GameName}#{TagLine}", request.GameName, request.TagLine);
                    return Results.Json(new { error = "Failed to verify Riot account", code = "RIOT_API_ERROR" }, statusCode: 503);
                }

                // Fetch summoner profile data (icon, level) - gracefully handle failures
                int? profileIconId = null;
                int? summonerLevel = null;
                try
                {
                    var summonerDoc = await riotApiClient.GetSummonerByPuuIdAsync(request.Region.ToLowerInvariant(), puuid);
                    if (summonerDoc != null)
                    {
                        var root = summonerDoc.RootElement;
                        if (root.TryGetProperty("profileIconId", out var iconProp))
                            profileIconId = iconProp.GetInt32();
                        if (root.TryGetProperty("summonerLevel", out var levelProp))
                            summonerLevel = (int)levelProp.GetInt64();
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail - profile data is optional
                    logger.LogWarning(ex, "Failed to fetch summoner profile data for {GameName}#{TagLine}", request.GameName, request.TagLine);
                }

                // Check if account is already linked
                var existingAccount = await riotAccountsRepo.GetByPuuidAsync(puuid);
                if (existingAccount != null)
                {
                    if (existingAccount.UserId != userId)
                    {
                        // Account belongs to another user
                        logger.LogWarning("Attempted to link already-linked account. PUUID: {Puuid}, existing user: {ExistingUserId}, requesting user: {UserId}",
                            puuid, existingAccount.UserId, userId);
                        return Results.Conflict(new { error = "This Riot account is already linked to another user", code = "ACCOUNT_ALREADY_LINKED" });
                    }

                    // Account already linked to this user - update it but preserve isPrimary and syncStatus
                    logger.LogInformation("Updating existing Riot account {GameName}#{TagLine} (PUUID: {Puuid}) for user {UserId}",
                        request.GameName, request.TagLine, puuid, userId);

                    var updatedAccount = new RiotAccount
                    {
                        Puuid = puuid,
                        UserId = userId.Value,
                        GameName = request.GameName,
                        TagLine = request.TagLine,
                        SummonerName = $"{request.GameName}#{request.TagLine}",
                        Region = request.Region.ToLowerInvariant(),
                        IsPrimary = existingAccount.IsPrimary,
                        SyncStatus = existingAccount.SyncStatus,
                        ProfileIconId = profileIconId ?? existingAccount.ProfileIconId,
                        SummonerLevel = summonerLevel ?? existingAccount.SummonerLevel,
                        LastSyncAt = existingAccount.LastSyncAt,
                        CreatedAt = existingAccount.CreatedAt,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await riotAccountsRepo.UpsertAsync(updatedAccount);

                    return Results.Ok(new LinkRiotAccountResponse(
                        puuid,
                        request.GameName,
                        request.TagLine,
                        request.Region.ToLowerInvariant(),
                        updatedAccount.IsPrimary,
                        updatedAccount.SyncStatus
                    ));
                }

                // New account - check if this is the user's first account (to set as primary)
                var existingAccounts = await riotAccountsRepo.GetByUserIdAsync(userId.Value);
                var isPrimary = existingAccounts.Count == 0;

                // Create the riot account record
                var riotAccount = new RiotAccount
                {
                    Puuid = puuid,
                    UserId = userId.Value,
                    GameName = request.GameName,
                    TagLine = request.TagLine,
                    SummonerName = $"{request.GameName}#{request.TagLine}",
                    Region = request.Region.ToLowerInvariant(),
                    IsPrimary = isPrimary,
                    SyncStatus = "pending",
                    ProfileIconId = profileIconId,
                    SummonerLevel = summonerLevel,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await riotAccountsRepo.UpsertAsync(riotAccount);
                logger.LogInformation("Linked Riot account {GameName}#{TagLine} (PUUID: {Puuid}) to user {UserId}",
                    request.GameName, request.TagLine, puuid, userId);

                return Results.Created($"{Route}/{puuid}", new LinkRiotAccountResponse(
                    puuid,
                    request.GameName,
                    request.TagLine,
                    request.Region.ToLowerInvariant(),
                    isPrimary,
                    "pending"
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in RiotAccountsEndpoint POST");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }

    private void ConfigureDeleteEndpoint(WebApplication app)
    {
        app.MapDelete(Route + "/{puuid}", [Authorize] async (
            string puuid,
            HttpContext httpContext,
            [FromServices] RiotAccountsRepository riotAccountsRepo,
            [FromServices] ILogger<RiotAccountsEndpoint> logger
        ) =>
        {
            try
            {
                var userId = GetUserId(httpContext);
                if (userId == null) return Results.Unauthorized();

                // Check if account exists and belongs to user
                var account = await riotAccountsRepo.GetByPuuidAsync(puuid);
                if (account == null || account.UserId != userId)
                {
                    return Results.NotFound(new { error = "Riot account not found", code = "ACCOUNT_NOT_FOUND" });
                }

                // Delete the account
                await riotAccountsRepo.DeleteAsync(puuid, userId.Value);
                logger.LogInformation("Unlinked Riot account {Puuid} from user {UserId}", puuid, userId);

                return Results.NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting Riot account {Puuid}", puuid);
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }

    private void ConfigureSyncEndpoint(WebApplication app)
    {
        app.MapPost(Route + "/{puuid}/sync", [Authorize] async (
            string puuid,
            HttpContext httpContext,
            [FromServices] RiotAccountsRepository riotAccountsRepo,
            [FromServices] IRiotApiClient riotApiClient,
            [FromServices] ILogger<RiotAccountsEndpoint> logger
        ) =>
        {
            try
            {
                var userId = GetUserId(httpContext);
                if (userId == null) return Results.Unauthorized();

                // Check if account exists and belongs to user
                var account = await riotAccountsRepo.GetByPuuidAsync(puuid);
                if (account == null || account.UserId != userId)
                {
                    return Results.NotFound(new { error = "Riot account not found", code = "ACCOUNT_NOT_FOUND" });
                }

                // Check if already syncing - don't allow re-queue
                if (account.SyncStatus == "syncing")
                {
                    return Results.Conflict(new { error = "Sync already in progress", code = "SYNC_IN_PROGRESS" });
                }

                // Set sync status to 'pending' to queue for background processing.
                // A background job (not yet implemented) will pick up accounts with
                // status='pending' and perform the actual match synchronization,
                // updating status to 'syncing' -> 'completed'/'failed'.
                await riotAccountsRepo.UpdateSyncStatusAsync(puuid, "pending");
                logger.LogInformation("Queued sync for Riot account {Puuid}, user {UserId}", puuid, userId);

                return Results.Accepted($"{Route}/{puuid}/sync-status", new SyncResponse(puuid, "pending", "Sync queued"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error triggering sync for Riot account {Puuid}", puuid);
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }

    private void ConfigureSyncStatusEndpoint(WebApplication app)
    {
        app.MapGet(Route + "/{puuid}/sync-status", [Authorize] async (
            string puuid,
            HttpContext httpContext,
            [FromServices] RiotAccountsRepository riotAccountsRepo,
            [FromServices] ILogger<RiotAccountsEndpoint> logger
        ) =>
        {
            try
            {
                var userId = GetUserId(httpContext);
                if (userId == null) return Results.Unauthorized();

                // Check if account exists and belongs to user
                var account = await riotAccountsRepo.GetByPuuidAsync(puuid);
                if (account == null || account.UserId != userId)
                {
                    return Results.NotFound(new { error = "Riot account not found", code = "ACCOUNT_NOT_FOUND" });
                }

                return Results.Ok(new SyncStatusResponse(
                    puuid,
                    account.SyncStatus,
                    account.LastSyncAt
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting sync status for Riot account {Puuid}", puuid);
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }

    private static long? GetUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return null;
        return userId;
    }

    public record LinkRiotAccountRequest(
        [property: JsonPropertyName("gameName")] string GameName,
        [property: JsonPropertyName("tagLine")] string TagLine,
        [property: JsonPropertyName("region")] string Region
    );

    public record LinkRiotAccountResponse(
        [property: JsonPropertyName("puuid")] string Puuid,
        [property: JsonPropertyName("gameName")] string GameName,
        [property: JsonPropertyName("tagLine")] string TagLine,
        [property: JsonPropertyName("region")] string Region,
        [property: JsonPropertyName("isPrimary")] bool IsPrimary,
        [property: JsonPropertyName("syncStatus")] string SyncStatus
    );

    public record SyncResponse(
        [property: JsonPropertyName("puuid")] string Puuid,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("message")] string Message
    );

    public record SyncStatusResponse(
        [property: JsonPropertyName("puuid")] string Puuid,
        [property: JsonPropertyName("syncStatus")] string SyncStatus,
        [property: JsonPropertyName("lastSyncAt")] DateTime? LastSyncAt
    );
}

