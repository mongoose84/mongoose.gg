using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Database.Repositories;
using Mongoose.Api.Infrastructure.Riot;

namespace Mongoose.Api.Application.Endpoints.Auth;

/// <summary>
/// Riot Accounts Endpoint
/// Provides all operations on linked Riot accounts:
/// - POST /api/v2/users/me/riot-accounts - Link a new Riot account
/// - DELETE /api/v2/users/me/riot-accounts/{puuid} - Unlink a Riot account
/// - PUT /api/v2/users/me/riot-accounts/{puuid}/primary - Set a linked account as primary
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
        ConfigureSetPrimaryEndpoint(app);
        ConfigureSyncEndpoint(app);
        ConfigureSyncStatusEndpoint(app);
    }

    private void ConfigureLinkEndpoint(WebApplication app)
    {
        _ = app.MapPost(Route, [Authorize] async (
            HttpContext httpContext,
            [FromBody] LinkRiotAccountRequest request,
            [FromServices] UsersRepository usersRepo,
            [FromServices] RiotAccountsRepository riotAccountsRepo,
            [FromServices] IUserRiotAccountsRepository userRiotAccountsRepo,
            [FromServices] IRiotApiClient riotApiClient,
            [FromServices] ILogger<RiotAccountsEndpoint> logger
        ) =>
        {
            try
            {
                var userId = GetUserId(httpContext);
                if (userId == null) return AuthResults.InvalidSession();

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
                    return AuthResults.InvalidSession();
                }

                // Lookup PUUID from Riot API
                string puuid;
                try
                {
                    puuid = await riotApiClient.GetPuuIdAsync(request.GameName, request.TagLine);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    logger.LogWarning("Riot account not found: {GameName}#{TagLine}",
                        LogSanitizer.Sanitize(request.GameName),
                        LogSanitizer.Sanitize(request.TagLine));
                    return Results.NotFound(new { error = "Riot account not found", code = "RIOT_ACCOUNT_NOT_FOUND" });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error looking up Riot account: {GameName}#{TagLine}",
                        LogSanitizer.Sanitize(request.GameName),
                        LogSanitizer.Sanitize(request.TagLine));
                    return Results.Json(new { error = "Failed to verify Riot account", code = "RIOT_API_ERROR" }, statusCode: 503);
                }

                // Check if user already has this account linked
                var alreadyLinked = await userRiotAccountsRepo.IsLinkedAsync(userId.Value, puuid);
                if (alreadyLinked)
                {
                    // Already linked to this user - just return success
                    var existingAccount = await riotAccountsRepo.GetByPuuidAsync(puuid);
                    var existingLinks = await userRiotAccountsRepo.GetByUserIdAsync(userId.Value);
                    var existingLink = existingLinks.FirstOrDefault(l => l.Link.Puuid == puuid);
                    // Defensive: If existingLink is null, default isPrimary to false
                    var existingIsPrimary = existingLink.Link != null && existingLink.Link.IsPrimary;
                    logger.LogInformation("Account {GameName}#{TagLine} already linked to user {UserId}",
                        LogSanitizer.Sanitize(request.GameName),
                        LogSanitizer.Sanitize(request.TagLine), userId);

                    return Results.Ok(new LinkRiotAccountResponse(
                        puuid,
                        existingAccount?.GameName ?? request.GameName,
                        existingAccount?.TagLine ?? request.TagLine,
                        existingAccount?.Region ?? request.Region.ToLowerInvariant(),
                        existingIsPrimary,
                        existingAccount?.SyncStatus ?? "pending"
                    ));
                }

                var userTier = user.Tier?.Trim().ToLowerInvariant() ?? "free";
                if (userTier == "free")
                {
                    var currentLinkCount = await userRiotAccountsRepo.GetLinkCountForUserAsync(userId.Value);
                    if (currentLinkCount >= 1)
                    {
                        return Results.BadRequest(new
                        {
                            error = "Free tier is limited to 1 linked account. Upgrade to Pro for unlimited accounts.",
                            code = "ACCOUNT_LIMIT_REACHED",
                            currentLimit = 1,
                            tier = "free"
                        });
                    }
                }

                // Fetch summoner profile data (icon, level, summonerId) - gracefully handle failures
                int? profileIconId = null;
                int? summonerLevel = null;
                string? summonerId = null;
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
                        if (root.TryGetProperty("id", out var idProp))
                            summonerId = idProp.GetString();
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail - profile data is optional
                    logger.LogWarning(ex, "Failed to fetch summoner profile data for {GameName}#{TagLine}",
                        LogSanitizer.Sanitize(request.GameName),
                        LogSanitizer.Sanitize(request.TagLine));
                }

                // Fetch ranked data using PUUID (standardized approach)
                string? soloTier = null, soloRank = null, flexTier = null, flexRank = null;
                int? soloLp = null, flexLp = null;
                try
                {
                    using var leagueDoc = await riotApiClient.GetLeagueEntriesByPuuidAsync(request.Region.ToLowerInvariant(), puuid);
                    foreach (var entry in leagueDoc.RootElement.EnumerateArray())
                    {
                        // Extract summonerId from league entry if available (for backwards compatibility)
                        if (summonerId == null && entry.TryGetProperty("summonerId", out var summonerIdProp))
                        {
                            summonerId = summonerIdProp.GetString();
                        }

                        var queueType = entry.GetProperty("queueType").GetString();
                        if (queueType == "RANKED_SOLO_5x5")
                        {
                            soloTier = entry.GetProperty("tier").GetString();
                            soloRank = entry.GetProperty("rank").GetString();
                            soloLp = entry.GetProperty("leaguePoints").GetInt32();
                        }
                        else if (queueType == "RANKED_FLEX_SR")
                        {
                            flexTier = entry.GetProperty("tier").GetString();
                            flexRank = entry.GetProperty("rank").GetString();
                            flexLp = entry.GetProperty("leaguePoints").GetInt32();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail - rank data is optional
                    logger.LogWarning(ex, "Failed to fetch ranked data for {GameName}#{TagLine}",
                        LogSanitizer.Sanitize(request.GameName),
                        LogSanitizer.Sanitize(request.TagLine));
                }

                // Check if this Riot account already exists (linked by another user)
                var existingRiotAccount = await riotAccountsRepo.GetByPuuidAsync(puuid);

                // Check if this is the user's first account (to set as primary)
                var userLinks = await userRiotAccountsRepo.GetByUserIdAsync(userId.Value);
                var isPrimary = userLinks.Count == 0;

                if (existingRiotAccount != null)
                {
                    // Account already exists - just link it to this user (M:M relationship)
                    await userRiotAccountsRepo.LinkAsync(userId.Value, puuid, isPrimary);

                    logger.LogInformation("Linked existing Riot account {GameName}#{TagLine} (PUUID: {Puuid}) to user {UserId}",
                        LogSanitizer.Sanitize(existingRiotAccount.GameName),
                        LogSanitizer.Sanitize(existingRiotAccount.TagLine), puuid, userId);

                    return Results.Created($"{Route}/{puuid}", new LinkRiotAccountResponse(
                        puuid,
                        existingRiotAccount.GameName,
                        existingRiotAccount.TagLine,
                        existingRiotAccount.Region,
                        isPrimary,
                        existingRiotAccount.SyncStatus
                    ));
                }

                // Create new Riot account record
                var riotAccount = new RiotAccount
                {
                    Puuid = puuid,
                    GameName = request.GameName,
                    TagLine = request.TagLine,
                    SummonerName = $"{request.GameName}#{request.TagLine}",
                    Region = request.Region.ToLowerInvariant(),
                    SummonerId = summonerId,
                    SyncStatus = "pending",
                    ProfileIconId = profileIconId,
                    SummonerLevel = summonerLevel,
                    SoloTier = soloTier,
                    SoloRank = soloRank,
                    SoloLp = soloLp,
                    FlexTier = flexTier,
                    FlexRank = flexRank,
                    FlexLp = flexLp,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await riotAccountsRepo.UpsertAsync(riotAccount);

                // Link the account to the user
                await userRiotAccountsRepo.LinkAsync(userId.Value, puuid, isPrimary);

                logger.LogInformation("Created and linked new Riot account {GameName}#{TagLine} (PUUID: {Puuid}) to user {UserId}",
                    LogSanitizer.Sanitize(request.GameName),
                    LogSanitizer.Sanitize(request.TagLine), puuid, userId);

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
            [FromServices] IUserRiotAccountsRepository userRiotAccountsRepo,
            [FromServices] ILogger<RiotAccountsEndpoint> logger
        ) =>
        {
            try
            {
                var userId = GetUserId(httpContext);
                if (userId == null) return AuthResults.InvalidSession();

                // Check if user has this account linked
                var isLinked = await userRiotAccountsRepo.IsLinkedAsync(userId.Value, puuid);
                if (!isLinked)
                {
                    return Results.NotFound(new { error = "Riot account not found", code = "ACCOUNT_NOT_FOUND" });
                }

                var linkedAccounts = await userRiotAccountsRepo.GetByUserIdAsync(userId.Value);
                var removedWasPrimary = linkedAccounts.Any(a =>
                    a.Link.Puuid.Equals(puuid, StringComparison.Ordinal) && a.Link.IsPrimary);
                var nextPrimaryPuuid = removedWasPrimary
                    ? linkedAccounts
                        .Where(a => !a.Link.Puuid.Equals(puuid, StringComparison.Ordinal))
                        .OrderBy(a => a.Link.LinkedAt)
                        .Select(a => a.Link.Puuid)
                        .FirstOrDefault()
                    : null;

                // Unlink the account from this user
                await userRiotAccountsRepo.UnlinkAsync(userId.Value, puuid);

                // If the removed account was primary, promote the next oldest linked account
                if (!string.IsNullOrWhiteSpace(nextPrimaryPuuid))
                {
                    await userRiotAccountsRepo.SetPrimaryAsync(userId.Value, nextPrimaryPuuid);
                }

                logger.LogInformation("Unlinked Riot account {Puuid} from user {UserId}", LogSanitizer.Sanitize(puuid), LogSanitizer.Sanitize(userId.ToString()));

                // Optionally: If no users are linked to this Riot account anymore, we could delete it
                // For now, keep it for historical match data
                // var hasAnyLinks = await userRiotAccountsRepo.HasAnyLinksAsync(puuid);
                // if (!hasAnyLinks) await riotAccountsRepo.DeleteAsync(puuid);

                return Results.NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting Riot account {Puuid}", LogSanitizer.Sanitize(puuid));
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }

    private void ConfigureSetPrimaryEndpoint(WebApplication app)
    {
        app.MapPut(Route + "/{puuid}/primary", [Authorize] async (
            string puuid,
            HttpContext httpContext,
            [FromServices] IUserRiotAccountsRepository userRiotAccountsRepo,
            [FromServices] ILogger<RiotAccountsEndpoint> logger
        ) =>
        {
            try
            {
                var userId = GetUserId(httpContext);
                if (userId == null) return AuthResults.InvalidSession();

                var isLinked = await userRiotAccountsRepo.IsLinkedAsync(userId.Value, puuid);
                if (!isLinked)
                {
                    return Results.NotFound(new { error = "Account not linked", code = "ACCOUNT_NOT_LINKED" });
                }

                await userRiotAccountsRepo.SetPrimaryAsync(userId.Value, puuid);
                logger.LogInformation("Set Riot account {Puuid} as primary for user {UserId}", LogSanitizer.Sanitize(puuid), LogSanitizer.Sanitize(userId.ToString()));

                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error setting primary Riot account {Puuid}", LogSanitizer.Sanitize(puuid));
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
            [FromServices] IUserRiotAccountsRepository userRiotAccountsRepo,
            [FromServices] IRiotApiClient riotApiClient,
            [FromServices] ILogger<RiotAccountsEndpoint> logger
        ) =>
        {
            try
            {
                var userId = GetUserId(httpContext);
                if (userId == null) return AuthResults.InvalidSession();

                // Check if user has this account linked
                var isLinked = await userRiotAccountsRepo.IsLinkedAsync(userId.Value, puuid);
                if (!isLinked)
                {
                    return Results.NotFound(new { error = "Riot account not found", code = "ACCOUNT_NOT_FOUND" });
                }

                // Get the account to check sync status
                var account = await riotAccountsRepo.GetByPuuidAsync(puuid);
                if (account == null)
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
                logger.LogInformation("Queued sync for Riot account {Puuid}, user {UserId}", LogSanitizer.Sanitize(puuid), LogSanitizer.Sanitize(userId.ToString()));

                return Results.Accepted($"{Route}/{puuid}/sync-status", new SyncResponse(puuid, "pending", "Sync queued"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error triggering sync for Riot account {Puuid}", LogSanitizer.Sanitize(puuid));
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
            [FromServices] IUserRiotAccountsRepository userRiotAccountsRepo,
            [FromServices] ILogger<RiotAccountsEndpoint> logger
        ) =>
        {
            try
            {
                var userId = GetUserId(httpContext);
                if (userId == null) return AuthResults.InvalidSession();

                // Check if user has this account linked
                var isLinked = await userRiotAccountsRepo.IsLinkedAsync(userId.Value, puuid);
                if (!isLinked)
                {
                    return Results.NotFound(new { error = "Riot account not found", code = "ACCOUNT_NOT_FOUND" });
                }

                var account = await riotAccountsRepo.GetByPuuidAsync(puuid);
                if (account == null)
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
                logger.LogError(ex, "Error getting sync status for Riot account {Puuid}", LogSanitizer.Sanitize(puuid));
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

