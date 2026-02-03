namespace RiotProxy.Core.Interfaces;

/// <summary>
/// Result of a GitHub issue creation operation.
/// </summary>
public record GitHubIssueResult(
    /// <summary>
    /// Whether the issue was created successfully
    /// </summary>
    bool Success,
    
    /// <summary>
    /// Error message if the operation failed (null on success)
    /// </summary>
    string? ErrorMessage = null
);

/// <summary>
/// Service for creating GitHub issues from user feedback.
/// This interface abstracts the GitHub API to allow testing and swapping implementations.
/// </summary>
public interface IGitHubService
{
    /// <summary>
    /// Creates a GitHub issue from user feedback.
    /// </summary>
    /// <param name="title">Issue title</param>
    /// <param name="body">Issue body in markdown format</param>
    /// <param name="labels">Labels to apply to the issue</param>
    /// <returns>Result indicating success or failure</returns>
    Task<GitHubIssueResult> CreateIssueAsync(string title, string body, IEnumerable<string> labels);
    
    /// <summary>
    /// Checks if the GitHub service is properly configured.
    /// </summary>
    /// <returns>True if the service can create issues</returns>
    bool IsConfigured { get; }
}

