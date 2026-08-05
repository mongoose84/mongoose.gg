namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// The Riot identity returned by Riot Sign-On after a successful authorization.
/// The PUUID is authoritative — it comes from Riot's own account endpoint using
/// the access token obtained server-side, never from client input.
/// Region is the player's active platform (from the RSO cpid claim) and may be null.
/// </summary>
public record RiotSignOnIdentity(string Puuid, string GameName, string TagLine, string? Region);

/// <summary>
/// Client for the Riot Sign-On (RSO) OAuth 2.0 authorization code flow.
/// </summary>
public interface IRiotSignOnClient
{
    /// <summary>
    /// Exchanges an authorization code for tokens and resolves the Riot identity
    /// (PUUID, game name, tag line) that authorized the request.
    /// </summary>
    /// <exception cref="HttpRequestException">When the token exchange or account lookup fails.</exception>
    Task<RiotSignOnIdentity> ExchangeCodeAsync(string code, CancellationToken ct = default);
}
