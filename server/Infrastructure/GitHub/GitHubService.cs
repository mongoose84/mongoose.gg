using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RiotProxy.Core.Interfaces;

namespace RiotProxy.Infrastructure.GitHub;

/// <summary>
/// GitHub service implementation for creating issues via the GitHub REST API.
/// Configuration is loaded from secure server-side settings (never from client).
/// </summary>
public sealed class GitHubService : IGitHubService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubService> _logger;
    private readonly string? _token;
    private readonly string? _owner;
    private readonly string? _repo;
    
    private const string GitHubApiBaseUrl = "https://api.github.com";
    
    public GitHubService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GitHubService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Load configuration from secure server-side settings
        // Priority: config keys -> env vars
        _token = configuration["GitHub:Token"] 
            ?? configuration["GITHUB_TOKEN"]
            ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            
        _owner = configuration["GitHub:Owner"]
            ?? configuration["GITHUB_OWNER"]
            ?? Environment.GetEnvironmentVariable("GITHUB_OWNER");
            
        _repo = configuration["GitHub:Repo"]
            ?? configuration["GITHUB_REPO"]
            ?? Environment.GetEnvironmentVariable("GITHUB_REPO");
        
        // Configure default headers
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("MongooseApp", "1.0"));
        
        if (!string.IsNullOrWhiteSpace(_token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _token);
        }
    }
    
    /// <inheritdoc />
    public bool IsConfigured => 
        !string.IsNullOrWhiteSpace(_token) && 
        !string.IsNullOrWhiteSpace(_owner) && 
        !string.IsNullOrWhiteSpace(_repo);
    
    /// <inheritdoc />
    public async Task<GitHubIssueResult> CreateIssueAsync(
        string title, 
        string body, 
        IEnumerable<string> labels)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("GitHub service is not configured. Cannot create issue.");
            return new GitHubIssueResult(false, "GitHub integration is not configured");
        }
        
        try
        {
            var requestBody = new
            {
                title,
                body,
                labels = labels.ToArray()
            };
            
            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{GitHubApiBaseUrl}/repos/{_owner}/{_repo}/issues";

            _logger.LogInformation("Creating GitHub issue in {Owner}/{Repo}", _owner, _repo);

            var response = await _httpClient.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully created GitHub issue");
                return new GitHubIssueResult(true);
            }
            
            // Handle specific error cases without leaking details
            var statusCode = (int)response.StatusCode;
            _logger.LogWarning(
                "GitHub API returned status {StatusCode} when creating issue", 
                statusCode);
            
            return statusCode switch
            {
                401 => new GitHubIssueResult(false, "GitHub authentication failed"),
                403 => new GitHubIssueResult(false, "GitHub access denied"),
                404 => new GitHubIssueResult(false, "GitHub repository not found"),
                422 => new GitHubIssueResult(false, "Invalid issue data"),
                429 => new GitHubIssueResult(false, "GitHub rate limit exceeded"),
                >= 500 => new GitHubIssueResult(false, "GitHub service unavailable"),
                _ => new GitHubIssueResult(false, "Failed to create feedback issue")
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error when creating GitHub issue");
            return new GitHubIssueResult(false, "Unable to connect to GitHub");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout when creating GitHub issue");
            return new GitHubIssueResult(false, "GitHub request timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when creating GitHub issue");
            return new GitHubIssueResult(false, "An unexpected error occurred");
        }
    }
}

