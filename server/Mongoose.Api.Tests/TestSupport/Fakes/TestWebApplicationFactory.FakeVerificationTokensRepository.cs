using System.Collections.Concurrent;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Infrastructure.Database.Repositories;

namespace Mongoose.Api.Tests;

internal sealed partial class TestWebApplicationFactory
{
    /// <summary>
    /// Fake verification tokens repository for testing.
    /// </summary>
    internal sealed class FakeVerificationTokensRepository : VerificationTokensRepository
    {
        private readonly ConcurrentDictionary<long, VerificationToken> _tokens = new();
        private long _nextId = 1;

        public FakeVerificationTokensRepository() : base(null!) { }

        public override Task<long> CreateTokenAsync(long userId, string tokenType, string code, DateTime expiresAt)
        {
            var token = new VerificationToken
            {
                Id = _nextId++,
                UserId = userId,
                TokenType = tokenType,
                Code = code,
                ExpiresAt = expiresAt,
                UsedAt = null,
                Attempts = 0,
                CreatedAt = DateTime.UtcNow
            };
            _tokens[token.Id] = token;
            return Task.FromResult(token.Id);
        }

        public override Task<VerificationToken?> GetActiveTokenAsync(long userId, string tokenType)
        {
            var token = _tokens.Values
                .Where(t => t.UserId == userId && t.TokenType == tokenType && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefault();
            return Task.FromResult(token);
        }

        public override Task MarkTokenAsUsedAsync(long tokenId)
        {
            if (_tokens.TryGetValue(tokenId, out var token))
            {
                token.UsedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public override Task IncrementAttemptsAsync(long tokenId)
        {
            if (_tokens.TryGetValue(tokenId, out var token))
            {
                token.Attempts++;
            }
            return Task.CompletedTask;
        }

        public override Task<int> CountRecentTokensAsync(long userId, string tokenType, int seconds)
        {
            var since = DateTime.UtcNow.AddSeconds(-seconds);
            var count = _tokens.Values.Count(t => t.UserId == userId && t.TokenType == tokenType && t.CreatedAt > since);
            return Task.FromResult(count);
        }

        public override Task InvalidateActiveTokensAsync(long userId, string tokenType)
        {
            foreach (var token in _tokens.Values.Where(t => t.UserId == userId && t.TokenType == tokenType && t.UsedAt == null))
            {
                token.UsedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public void AddToken(long userId, string tokenType, string code, DateTime expiresAt)
        {
            var token = new VerificationToken
            {
                Id = _nextId++,
                UserId = userId,
                TokenType = tokenType,
                Code = code,
                ExpiresAt = expiresAt,
                UsedAt = null,
                Attempts = 0,
                CreatedAt = DateTime.UtcNow
            };
            _tokens[token.Id] = token;
        }

        public VerificationToken? GetToken(long tokenId)
        {
            _tokens.TryGetValue(tokenId, out var token);
            return token;
        }

        public IEnumerable<VerificationToken> GetAllTokensForUser(long userId)
        {
            return _tokens.Values.Where(t => t.UserId == userId);
        }

        /// <summary>
        /// Helper method for testing: sets the attempt count on a token.
        /// </summary>
        public void SetTokenAttempts(long tokenId, int attempts)
        {
            if (_tokens.TryGetValue(tokenId, out var token))
            {
                token.Attempts = attempts;
            }
        }
    }
}
