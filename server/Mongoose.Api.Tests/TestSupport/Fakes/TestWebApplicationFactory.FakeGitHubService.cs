using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Tests;

internal sealed partial class TestWebApplicationFactory
{
    /// <summary>
    /// Fake GitHub service for testing feedback endpoint.
    /// </summary>
    internal sealed class FakeGitHubService : IGitHubService
    {
        private readonly List<CreatedIssue> _createdIssues = new();
        private bool _shouldFail;
        private string? _failureMessage;
        private bool _isConfigured = true;

        public bool IsConfigured => _isConfigured;

        public IReadOnlyList<CreatedIssue> CreatedIssues => _createdIssues;

        public Task<GitHubIssueResult> CreateIssueAsync(string title, string body, IEnumerable<string> labels)
        {
            if (!_isConfigured)
            {
                return Task.FromResult(new GitHubIssueResult(false, "GitHub integration is not configured"));
            }

            if (_shouldFail)
            {
                return Task.FromResult(new GitHubIssueResult(false, _failureMessage ?? "Simulated failure"));
            }

            _createdIssues.Add(new CreatedIssue(title, body, labels.ToArray()));
            return Task.FromResult(new GitHubIssueResult(true));
        }

        /// <summary>
        /// Configures the fake to return a failure on the next issue creation.
        /// </summary>
        public void SetupFailure(string? message = null)
        {
            _shouldFail = true;
            _failureMessage = message;
        }

        /// <summary>
        /// Configures the fake to simulate an unconfigured GitHub service.
        /// </summary>
        public void SetupNotConfigured()
        {
            _isConfigured = false;
        }

        /// <summary>
        /// Resets the fake to its default state.
        /// </summary>
        public void Reset()
        {
            _shouldFail = false;
            _failureMessage = null;
            _isConfigured = true;
            _createdIssues.Clear();
        }

        public record CreatedIssue(string Title, string Body, string[] Labels);
    }
}
