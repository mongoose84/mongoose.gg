using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Infrastructure.Google;

/// <summary>
/// Google Sign-On OAuth 2.0 client.
/// Exchanges the authorization code at Google's token endpoint (confidential client,
/// client_id/client_secret in the POST body) and resolves the authorizing user's
/// identity via the userinfo endpoint using the access token.
/// Requires Google OAuth client credentials (Auth:Google:ClientId/ClientSecret
/// or GSO_CLIENT_ID/GSO_CLIENT_SECRET environment variables).
/// </summary>
public class GoogleSignOnClient : IGoogleSignOnClient
{
    private const string DefaultTokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string DefaultUserInfoEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo";

    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public GoogleSignOnClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public virtual async Task<GoogleSignOnIdentity> ExchangeCodeAsync(string code, CancellationToken ct = default)
    {
        var clientId = _config.GetValue<string>("Auth:Google:ClientId") ?? _config.GetValue<string>("GSO_CLIENT_ID");
        var clientSecret = _config.GetValue<string>("Auth:Google:ClientSecret") ?? _config.GetValue<string>("GSO_CLIENT_SECRET");
        var redirectUri = _config.GetValue<string>("Auth:Google:RedirectUri");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(redirectUri))
        {
            throw new InvalidOperationException("Google Sign-On is not configured (client id, client secret, and redirect URI are required).");
        }

        var tokenEndpoint = _config.GetValue<string>("Auth:Google:TokenEndpoint") ?? DefaultTokenEndpoint;

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            })
        };

        using var tokenResponse = await _http.SendAsync(tokenRequest, ct);
        tokenResponse.EnsureSuccessStatusCode();

        string? accessToken;
        using (var tokenDoc = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(ct)))
        {
            accessToken = tokenDoc.RootElement.TryGetProperty("access_token", out var tokenProp)
                ? tokenProp.GetString()
                : null;
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new HttpRequestException("Google Sign-On token response did not contain an access token.");
        }

        return await GetIdentityAsync(accessToken, ct);
    }

    private async Task<GoogleSignOnIdentity> GetIdentityAsync(string accessToken, CancellationToken ct)
    {
        var userInfoEndpoint = _config.GetValue<string>("Auth:Google:UserInfoEndpoint") ?? DefaultUserInfoEndpoint;

        using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, userInfoEndpoint);
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var userInfoResponse = await _http.SendAsync(userInfoRequest, ct);
        userInfoResponse.EnsureSuccessStatusCode();

        using var userInfoDoc = JsonDocument.Parse(await userInfoResponse.Content.ReadAsStringAsync(ct));
        var root = userInfoDoc.RootElement;

        var sub = root.TryGetProperty("sub", out var subProp) ? subProp.GetString() : null;
        if (string.IsNullOrWhiteSpace(sub))
        {
            throw new HttpRequestException("Google Sign-On userinfo response did not contain a sub claim.");
        }

        var email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new HttpRequestException("Google Sign-On userinfo response did not contain an email.");
        }

        var emailVerified = root.TryGetProperty("email_verified", out var emailVerifiedProp)
            && emailVerifiedProp.ValueKind == JsonValueKind.True;
        var name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;

        return new GoogleSignOnIdentity(sub, email, emailVerified, name);
    }
}
