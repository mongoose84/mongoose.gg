namespace RiotProxy.Application.DTOs
{
    public static class DuoRoleConsistencyDto
    {
        public record DuoRoleConsistencyResponse(
            IList<PlayerRoleDistribution> Players
        );

        public record PlayerRoleDistribution(
            string PlayerName,
            IList<RoleStats> Roles
        );

        public record RoleStats(
            string Position,
            int GamesPlayed,
            double Percentage
        );
    }
}

