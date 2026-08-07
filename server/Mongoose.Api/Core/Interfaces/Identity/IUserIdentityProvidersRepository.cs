namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// Maps external identity provider logins (Riot Sign-On, Google Sign-On, ...) to local
/// users. Generic over provider name and provider-specific identity id (puuid, sub, ...)
/// so a new provider needs no schema change — just a new "provider" value.
/// </summary>
public interface IUserIdentityProvidersRepository
{
    /// <summary>
    /// Returns the id of the user linked to the given provider identity, or null if
    /// no user has linked this identity yet.
    /// </summary>
    Task<long?> GetUserIdByProviderIdentityAsync(string provider, string providerUid);

    /// <summary>
    /// Links a provider identity to a user. A user may have more than one linked
    /// provider identity (e.g. both Riot and Google).
    /// </summary>
    Task LinkProviderIdentityAsync(long userId, string provider, string providerUid);
}
