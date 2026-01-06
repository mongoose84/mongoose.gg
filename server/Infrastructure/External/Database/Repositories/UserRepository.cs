using MySqlConnector;
using RiotProxy.External.Domain.Enums;
using RiotProxy.External.Domain.Entities;

namespace RiotProxy.Infrastructure.External.Database.Repositories
{
    public class UserRepository : RepositoryBase
    {
        public UserRepository(IDbConnectionFactory factory) : base(factory)
        {
        }

        public async Task<IList<User>> GetAllUsersAsync()
        {
            const string sql = "SELECT UserId, UserName, UserType FROM User";
            return await ExecuteListAsync(sql, r => new User
            {
                UserId = r.GetInt32(0),
                UserName = r.GetString(1),
                UserType = (UserTypeEnum)r.GetInt32(2)
            });
        }

        public async Task<User?> GetByUserNameAsync(string userName)
        {
            const string sql = "SELECT UserId, UserName, UserType FROM User WHERE UserName = @userName";
            return await ExecuteSingleAsync(sql, r => new User
            {
                UserId = r.GetInt32(0),
                UserName = r.GetString(1),
                UserType = Enum.Parse<UserTypeEnum>(r.GetString(2))
            }, ("@userName", userName));
        }
        
        public async Task<User?> CreateUserAsync(string userName, UserTypeEnum userType)
        {
            const string sql = @"
                INSERT INTO User (UserName, UserType)          
                VALUES (@userName, @userType);
                SELECT LAST_INSERT_ID();             
            ";
            
            var result = await ExecuteScalarAsync<ulong>(sql, ("@userName", userName), ("@userType", (int)userType));
            
            if (result == 0)
                return null;

            return new User
            {
                UserId = (int)result,
                UserName = userName,
                UserType = userType
            };
        }
    }
}