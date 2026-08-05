using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Infrastructure.Riot;

/// <summary>
/// Riot Sign-On (RSO) OAuth 2.0 client.
/// Exchanges the authorization code at Riot's token endpoint (confidential client,
/// Basic auth with client credentials) and resolves the authorizing player's
/// identity via the account-v1 "accounts/me" endpoint using the access token.
/// Requires RSO client credentials issued by Riot (Auth:Riot:ClientId/ClientSecret
/// or RSO_CLIENT_ID/RSO_CLIENT_SECRET environment variables).
/// </summary>
public class RiotSignOnClient : IRiotSignOnClient
{
    private const string DefaultTokenEndpoint = "https://auth.riotgames.com/token";
    private const string DefaultAccountEndpoint = "https://europe.api.riotgames.com/riot/account/v1/accounts/me";

    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public RiotSignOnClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public virtual async Task<RiotSignOnIdentity> ExchangeCodeAsync(string code, CancellationToken ct = default)
    {
        var clientId = _config.GetValue<string>("Auth:Riot:ClientId") ?? _config.GetValue<string>("RSO_CLIENT_ID");
        var clientSecret = _config.GetValue<string>("Auth:Riot:ClientSecret") ?? _config.GetValue<string>("RSO_CLIENT_SECRET");
        var redirectUri = _config.GetValue<string>("Auth:Riot:RedirectUri");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(redirectUri))
        {
            throw new InvalidOperationException("Riot Sign-On is not configured (client id, client secret, and redirect URI are required).");
        }

        var tokenEndpoint = _config.GetValue<string>("Auth:Riot:TokenEndpoint") ?? DefaultTokenEndpoint;

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            })
        };
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));

        using var tokenResponse = await _http.SendAsync(tokenRequest, ct);
        tokenResponse.EnsureSuccessStatusCode();

        string? accessToken;
        string? idToken;
        using (var tokenDoc = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(ct)))
        {
            accessToken = tokenDoc.RootElement.TryGetProperty("access_token", out var tokenProp)
                ? tokenProp.GetString()
                : null;
            idToken = tokenDoc.RootElement.TryGetProperty("id_token", out var idTokenProp)
                ? idTokenProp.GetString()
                : null;
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new HttpRequestException("Riot Sign-On token response did not contain an access token.");
        }

        // The cpid claim carries the player's active platform (e.g. "EUW1"). It is
        // informational only (used to preselect the region) — identity comes from
        // the account endpoint below, so no signature validation is needed here.
        var region = TryReadCpidClaim(idToken);

        return await GetIdentityAsync(accessToken, region, ct);
    }

    private static string? TryReadCpidClaim(string? idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return null;
        }

        var segments = idToken.Split('.');
        if (segments.Length < 2)
        {
            return null;
        }

        try
        {
            var payload = segments[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
            var cpid = doc.RootElement.TryGetProperty("cpid", out var cpidProp) ? cpidProp.GetString() : null;
            return string.IsNullOrWhiteSpace(cpid) ? null : cpid.ToLowerInvariant();
        }
        catch (FormatException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<RiotSignOnIdentity> GetIdentityAsync(string accessToken, string? region, CancellationToken ct)
    {
        var accountEndpoint = _config.GetValue<string>("Auth:Riot:AccountEndpoint") ?? DefaultAccountEndpoint;

        using var accountRequest = new HttpRequestMessage(HttpMethod.Get, accountEndpoint);
        accountRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var accountResponse = await _http.SendAsync(accountRequest, ct);
        accountResponse.EnsureSuccessStatusCode();

        using var accountDoc = JsonDocument.Parse(await accountResponse.Content.ReadAsStringAsync(ct));
        var root = accountDoc.RootElement;

        var puuid = root.TryGetProperty("puuid", out var puuidProp) ? puuidProp.GetString() : null;
        if (string.IsNullOrWhiteSpace(puuid))
        {
            throw new HttpRequestException("Riot Sign-On account response did not contain a PUUID.");
        }

        var gameName = root.TryGetProperty("gameName", out var gameNameProp) ? gameNameProp.GetString() : null;
        var tagLine = root.TryGetProperty("tagLine", out var tagLineProp) ? tagLineProp.GetString() : null;

        return new RiotSignOnIdentity(puuid, gameName ?? string.Empty, tagLine ?? string.Empty, region);
    }
}
