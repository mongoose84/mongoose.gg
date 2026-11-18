using System.Collections.Concurrent;
using Mongoose.Api.Core.Entities;
using Mongoose.Api.Infrastructure.Database.Repositories;

namespace Mongoose.Api.Tests;

internal sealed partial class TestWebApplicationFactory
{
    internal sealed class FakeUsersRepository : UsersRepository
    {
        private readonly ConcurrentDictionary<string, User> _usersByUsername = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, User> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<long, User> _usersById = new();
        private long _nextId = 1;

        public FakeUsersRepository() : base(null!, new FakeEncryptor())
        {
            // Pre-populate with a test user (password: "test-password")
            var testUser = new User
            {
                UserId = _nextId++,
                Username = "tester",
                Email = "tester@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("test-password"),
                SecurityStamp = Guid.NewGuid().ToString(),
                EmailVerified = true,
                IsActive = true,
                Tier = "free",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _usersByUsername["tester"] = testUser;
            _usersByEmail["tester@test.com"] = testUser;
            _usersById[testUser.UserId] = testUser;
        }

        public override Task<User?> GetByUsernameAsync(string username)
        {
            _usersByUsername.TryGetValue(username, out var user);
            return Task.FromResult(user);
        }

        public override Task<User?> GetByEmailAsync(string email)
        {
            _usersByEmail.TryGetValue(email, out var user);
            return Task.FromResult(user);
        }

        public override Task<long> UpsertAsync(User user)
        {
            if (user.UserId == 0)
            {
                user.UserId = _nextId++;
            }
            _usersByUsername[user.Username] = user;
            _usersByEmail[user.Email] = user;
            _usersById[user.UserId] = user;
            return Task.FromResult(user.UserId);
        }

        public override Task<User?> GetByIdAsync(long userId)
        {
            _usersById.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }

        public override Task UpdateEmailVerifiedAsync(long userId, bool verified)
        {
            if (_usersById.TryGetValue(userId, out var user))
            {
                user.EmailVerified = verified;
            }
            return Task.CompletedTask;
        }

        public override Task UpdateUserIconIdAsync(long userId, int? userIconId)
        {
            if (_usersById.TryGetValue(userId, out var user))
            {
                user.UserIconId = userIconId;
            }

            return Task.CompletedTask;
        }

        public override Task UpdatePasswordHashAsync(long userId, string passwordHash)
        {
            if (_usersById.TryGetValue(userId, out var user))
            {
                user.PasswordHash = passwordHash;
                user.SecurityStamp = Guid.NewGuid().ToString();
            }
            return Task.CompletedTask;
        }

        public override Task<string?> GetSecurityStampAsync(long userId)
        {
            _usersById.TryGetValue(userId, out var user);
            return Task.FromResult(user?.SecurityStamp);
        }

        public override Task<bool> DeleteUserAsync(long userId)
        {
            if (!_usersById.TryRemove(userId, out var removedUser))
            {
                return Task.FromResult(false);
            }

            _usersByUsername.TryRemove(removedUser.Username, out _);
            _usersByEmail.TryRemove(removedUser.Email, out _);
            return Task.FromResult(true);
        }

        public void AddUnverifiedUser(string username, string email, string password)
        {
            var user = new User
            {
                UserId = _nextId++,
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                SecurityStamp = Guid.NewGuid().ToString(),
                EmailVerified = false,
                IsActive = true,
                Tier = "free",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _usersByUsername[username] = user;
            _usersByEmail[email] = user;
            _usersById[user.UserId] = user;
        }

        public void AddInactiveUser(string username, string email, string password)
        {
            var user = new User
            {
                UserId = _nextId++,
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                SecurityStamp = Guid.NewGuid().ToString(),
                EmailVerified = true,
                IsActive = false,
                Tier = "free",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _usersByUsername[username] = user;
            _usersByEmail[email] = user;
            _usersById[user.UserId] = user;
        }

        public void SetSecurityStamp(string username, string securityStamp)
        {
            if (_usersByUsername.TryGetValue(username, out var user))
            {
                user.SecurityStamp = securityStamp;
            }
        }

        public void SetTier(string username, string tier)
        {
            if (_usersByUsername.TryGetValue(username, out var user))
            {
                user.Tier = tier;
            }
        }

        public void SetUserIconId(string username, int? userIconId)
        {
            if (_usersByUsername.TryGetValue(username, out var user))
            {
                user.UserIconId = userIconId;
            }
        }

        public override Task<bool> UsernameExistsAsync(string username)
        {
            return Task.FromResult(_usersByUsername.ContainsKey(username));
        }

        public override Task<bool> EmailExistsAsync(string email)
        {
            return Task.FromResult(_usersByEmail.ContainsKey(email));
        }

        public override Task<long> GetActiveUserCountAsync()
        {
            var count = _usersById.Values.Count(u => u.IsActive);
            return Task.FromResult((long)count);
        }
    }
}
