namespace RiotProxy.Application.DTOs
{
    public static class DuoKillEfficiencyDto
    {
        public record DuoKillEfficiencyResponse(
            IList<PlayerEfficiency> Players
        );

        public record PlayerEfficiency(
            string PlayerName,
            double KillParticipation,
            double DeathShareInLosses
        );
    }
}

