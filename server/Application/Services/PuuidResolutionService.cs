using Mongoose.Api.Core.Entities;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Application.Services;

/// <summary>
/// Service for resolving a user's primary Riot account PUUID.
/// Centralizes PUUID resolution logic that was duplicated across 15+ endpoints.
/// </summary>
public sealed class PuuidResolutionService
{
    private readonly IUserRiotAccountsRepository _userRiotAccountsRepo;
    private readonly IUsersRepository _usersRepository;
    private readonly ILogger<PuuidResolutionService> _logger;

    public PuuidResolutionService(
        IUserRiotAccountsRepository userRiotAccountsRepo,
        IUsersRepository usersRepository,
        ILogger<PuuidResolutionService> logger)
    {
        _userRiotAccountsRepo = userRiotAccountsRepo;
        _usersRepository = usersRepository;
        _logger = logger;
    }

    /// <summary>
    /// Represents the result of PUUID resolution with full account data.
    /// </summary>
    /// <param name="Account">The full Riot account entity with rank and profile data</param>
    /// <param name="IsPrimary">Whether this is the user's primary account</param>
    public record ResolvedAccount(RiotAccount Account, bool IsPrimary);

    /// <summary>
    /// Resolves the primary Riot account for a user.
    /// Returns the primary linked account if one exists, or the first linked account otherwise.
    /// </summary>
    /// <param name="userId">The user ID to resolve account for</param>
    /// <returns>
    /// A tuple containing either (null, ResolvedAccount) on success or (IResult, null) on failure.
    /// Failure can occur if the user has no linked Riot accounts.
    /// </returns>
    public async Task<(IResult? ErrorResult, ResolvedAccount? Account)> ResolvePrimaryAccountAsync(long userId)
    {
        var linkedAccounts = await GetVisibleLinkedAccountsAsync(userId);

        if (linkedAccounts == null || linkedAccounts.Count == 0)
        {
            _logger.LogWarning("No riot accounts found for userId {UserId}", userId);
            return (
                Results.NotFound(new { error = "No riot accounts found for this user", code = "RIOT_ACCOUNT_NOT_FOUND" }),
                null
            );
        }

        // Use primary account if one is marked, otherwise use the first account
        var primaryLink = linkedAccounts.FirstOrDefault(la => la.Link?.IsPrimary == true);
        if (primaryLink.Link?.IsPrimary == true)
        {
            return (null, new ResolvedAccount(primaryLink.Account, true));
        }

        // Fallback to first account (not marked as primary)
        var firstAccount = linkedAccounts[0].Account;
        return (null, new ResolvedAccount(firstAccount, false));
    }

    /// <summary>
    /// Resolves the primary PUUID for a user (lightweight operation).
    /// Use this when you only need the PUUID and don't need rank/profile data.
    /// </summary>
    /// <param name="userId">The user ID to resolve PUUID for</param>
    /// <returns>
    /// A tuple containing either (null, string) on success or (IResult, null) on failure.
    /// </returns>
    public async Task<(IResult? ErrorResult, string? Puuid)> ResolvePrimaryPuuidAsync(long userId)
    {
        var (errorResult, account) = await ResolvePrimaryAccountAsync(userId);
        if (errorResult != null)
        {
            return (errorResult, null);
        }

        return (null, account!.Account.Puuid);
    }

    /// <summary>
    /// Resolves all linked Riot accounts for a user.
    /// Useful for endpoints that need to query data across all linked accounts.
    /// </summary>
    /// <param name="userId">The user ID to resolve accounts for</param>
    /// <returns>
    /// A tuple containing either (null, List&lt;ResolvedAccount&gt;) on success or (IResult, null) on failure.
    /// </returns>
    public async Task<(IResult? ErrorResult, List<ResolvedAccount>? Accounts)> ResolveAllAccountsAsync(long userId)
    {
        var linkedAccounts = await GetVisibleLinkedAccountsAsync(userId);

        if (linkedAccounts == null || linkedAccounts.Count == 0)
        {
            _logger.LogWarning("No riot accounts found for userId {UserId}", userId);
            return (
                Results.NotFound(new { error = "No riot accounts found for this user", code = "RIOT_ACCOUNT_NOT_FOUND" }),
                null
            );
        }

        var accounts = linkedAccounts
            .Select(la => new ResolvedAccount(la.Account, la.Link?.IsPrimary == true))
            .ToList();

        return (null, accounts);
    }

    /// <summary>
    /// Verifies that a PUUID belongs to a specific user.
    /// Used by endpoints like MatchDetailsEndpoint that require PUUID ownership validation.
    /// </summary>
    /// <param name="userId">The user ID to verify ownership for</param>
    /// <param name="puuid">The PUUID to verify</param>
    /// <returns>
    /// True if the PUUID is linked to the user, false otherwise.
    /// </returns>
    public async Task<bool> VerifyPuuidOwnershipAsync(long userId, string puuid)
    {
        var visibleAccounts = await GetVisibleLinkedAccountsAsync(userId);
        return visibleAccounts.Any(linkedAccount =>
            string.Equals(linkedAccount.Account.Puuid, puuid, StringComparison.Ordinal));
    }

    private async Task<IList<(UserRiotAccountLink Link, RiotAccount Account)>> GetVisibleLinkedAccountsAsync(long userId)
    {
        var linkedAccounts = await _userRiotAccountsRepo.GetByUserIdAsync(userId);
        if (linkedAccounts == null)
        {
            return [];
        }

        if (linkedAccounts.Count == 0)
        {
            return linkedAccounts;
        }

        var user = await _usersRepository.GetByIdAsync(userId);
        var normalizedTier = NormalizeTier(user?.Tier);
        if (normalizedTier != "free")
        {
            return linkedAccounts;
        }

        var primaryOnlyAccounts = linkedAccounts
            .Where(linkedAccount => linkedAccount.Link?.IsPrimary == true)
            .ToList();

        if (primaryOnlyAccounts.Count > 0)
        {
            return primaryOnlyAccounts;
        }

        return [linkedAccounts[0]];
    }

    private static string NormalizeTier(string? tier)
    {
        return tier?.Trim().ToLowerInvariant() ?? "free";
    }
}