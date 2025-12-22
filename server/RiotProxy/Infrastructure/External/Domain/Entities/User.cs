using RiotProxy.External.Domain.Enums;

namespace RiotProxy.External.Domain.Entities
{
    public class User : EntityBase
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;    

        public UserTypeEnum UserType { get; set; } = UserTypeEnum.Solo;  
    }
}