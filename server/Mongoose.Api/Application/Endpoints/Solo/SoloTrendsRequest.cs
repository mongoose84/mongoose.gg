using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Application.Services;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services.Solo;

namespace Mongoose.Api.Application.Endpoints.Solo;

/// <summary>The resolved scope of a Solo trend request: whose matches, which queue and which range.</summary>
public sealed record SoloTrendsScope(long UserId, IReadOnlyList<string> Puuids, string QueueType, SoloRange Range, bool SingleAccount)
{
    public string RangeKey => SoloScope.RangeKey(Range);
}

/// <summary>
/// Shared request handling for the Solo trend endpoints (features/solo-trends.spec.md): authentication and
/// ownership, account resolution (never a PUUID from the client), and the queue and range parameters.
/// </summary>
internal static class SoloTrendsRequest
{
    public static async Task<(IResult? Error, SoloTrendsScope? Scope)> ResolveAsync(
        HttpContext httpContext,
        string userId,
        string? queueType,
        string? range,
        string? accountId,
        PuuidResolutionService puuidResolutionService,
        ISoloTrendsRepository repository,
        ILogger logger)
    {
        var (authError, authorizedUser) = AuthorizationHelper.ValidateAndGetUser(httpContext, userId, logger);
        if (authError != null) return (authError, null);

        if (!SoloScope.TryParseQueue(queueType, out var queue))
        {
            logger.LogWarning("Solo trends: invalid queueType {Queue}", LogSanitizer.Sanitize(queueType));
            return (Results.BadRequest(new { error = "Invalid queueType. Must be 'ranked_solo', 'ranked_flex' or 'all'.", code = "INVALID_QUEUE" }), null);
        }

        if (!SoloScope.TryParseRange(range, out var parsedRange))
        {
            logger.LogWarning("Solo trends: invalid range {Range}", LogSanitizer.Sanitize(range));
            return (Results.BadRequest(new { error = "Invalid range. Must be 'last20', 'last50' or 'season'.", code = "INVALID_RANGE" }), null);
        }

        var (accountError, resolvedAccounts) = await puuidResolutionService.ResolveRequestedAccountsAsync(authorizedUser!.UserId, accountId);
        if (accountError != null) return (accountError, null);

        if (resolvedAccounts == null || resolvedAccounts.Count == 0)
        {
            return (Results.NotFound(new { error = "No riot accounts found for this user", code = "RIOT_ACCOUNT_NOT_FOUND" }), null);
        }

        var puuids = resolvedAccounts.Select(a => a.Account.Puuid).ToList();

        // FR2: without a queue every endpoint picks the same default, so the page's four cards agree.
        queue ??= SoloScope.DefaultQueue(await repository.GetSeasonQueueCountsAsync(puuids));

        return (null, new SoloTrendsScope(authorizedUser.UserId, puuids, queue, parsedRange, puuids.Count == 1));
    }
}
