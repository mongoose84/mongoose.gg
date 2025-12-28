namespace RiotProxy.Application.DTOs
{
    public static class DuoLaneMatchupDto
    {
        public record DuoLaneMatchupResponse(
            IList<LaneComboStats> LaneCombos
        );

        public record LaneComboStats(
            string LaneCombo,
            int GamesPlayed,
            int Wins,
            double Winrate
        );
    }
}

