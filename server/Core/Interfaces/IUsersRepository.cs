using RiotProxy.Core.Entities;

namespace RiotProxy.Core.Interfaces;

public interface IUsersRepository
{
    Task<long> UpsertAsync(User user);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(long userId);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task<long> GetActiveUserCountAsync();
    Task UpdateEmailVerifiedAsync(long userId, bool verified);
}

