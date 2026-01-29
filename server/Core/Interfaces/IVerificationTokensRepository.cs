using RiotProxy.Core.Entities;

namespace RiotProxy.Core.Interfaces;

public interface IVerificationTokensRepository
{
    Task<long> CreateTokenAsync(long userId, string tokenType, string code, DateTime expiresAt);
    Task<VerificationToken?> GetActiveTokenAsync(long userId, string tokenType);
    Task MarkTokenAsUsedAsync(long tokenId);
    Task IncrementAttemptsAsync(long tokenId);
    Task<int> CountRecentTokensAsync(long userId, string tokenType, int seconds);
    Task InvalidateActiveTokensAsync(long userId, string tokenType);
}

