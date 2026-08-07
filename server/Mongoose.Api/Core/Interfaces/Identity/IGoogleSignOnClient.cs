namespace Mongoose.Api.Core.Interfaces;

/// <summary>
/// The Google identity returned by Google Sign-On after a successful authorization.
/// GoogleId (the "sub" claim) is authoritative — it comes from Google's own userinfo
/// endpoint using the access token obtained server-side, never from client input.
/// Email is only usable for account linking when EmailVerified is true, since Google
/// allows unverified emails on some account types.
/// </summary>
public record GoogleSignOnIdentity(string GoogleId, string Email, bool EmailVerified, string? Name);

/// <summary>
/// Client for the Google Sign-On OAuth 2.0 authorization code flow.
/// </summary>
public interface IGoogleSignOnClient
{
    /// <summary>
    /// Exchanges an authorization code for tokens and resolves the Google identity
    /// (sub, email, email_verified, name) that authorized the request.
    /// </summary>
    /// <exception cref="HttpRequestException">When the token exchange or userinfo lookup fails.</exception>
    Task<GoogleSignOnIdentity> ExchangeCodeAsync(string code, CancellationToken ct = default);
}
