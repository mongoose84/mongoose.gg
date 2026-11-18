using Mongoose.Api.Infrastructure.Email;

namespace Mongoose.Api.Tests;

internal sealed partial class TestWebApplicationFactory
{
    /// <summary>
    /// Fake email service for testing.
    /// </summary>
    internal sealed class FakeEmailService : IEmailService
    {
        private readonly List<SentEmail> _sentEmails = new();
        private readonly List<SentPasswordResetEmail> _sentPasswordResetEmails = new();
        private readonly SemaphoreSlim _verificationEmailSignal = new(0);

        public IReadOnlyList<SentEmail> SentEmails => _sentEmails;
        public IReadOnlyList<SentPasswordResetEmail> SentPasswordResetEmails => _sentPasswordResetEmails;

        public Task SendVerificationEmailAsync(string toEmail, string username, string verificationCode)
        {
            _sentEmails.Add(new SentEmail(toEmail, username, verificationCode));
            _verificationEmailSignal.Release();
            return Task.CompletedTask;
        }

        public Task SendPasswordResetEmailAsync(string toEmail, string username, string resetCode)
        {
            _sentPasswordResetEmails.Add(new SentPasswordResetEmail(toEmail, username, resetCode));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Waits until a verification email has been recorded. Use this instead of Task.Delay
        /// to create a deterministic sync point for fire-and-forget email sends.
        /// </summary>
        public Task WaitForVerificationEmailAsync(TimeSpan? timeout = null) =>
            _verificationEmailSignal.WaitAsync(timeout ?? TimeSpan.FromSeconds(5));

        public void Clear()
        {
            _sentEmails.Clear();
            _sentPasswordResetEmails.Clear();
        }

        public record SentEmail(string ToEmail, string Username, string VerificationCode);
        public record SentPasswordResetEmail(string ToEmail, string Username, string ResetCode);
    }
}
