namespace RiotProxy.External.Domain.Entities
{
    public class LolMatchParticipant : EntityBase
    {
        public string MatchId { get; set; } = string.Empty;
        public string PuuId { get; set; } = string.Empty;
        public int TeamId { get; set; }
        public bool Win { get; set; }
        public string Role { get; set; } = string.Empty;
        public string TeamPosition { get; set; } = string.Empty;
        public string Lane { get; set; } = string.Empty;
        public int ChampionId { get; set; }
        public string ChampionName { get; set; } = string.Empty;
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public int DoubleKills { get; set; }
        public int TripleKills { get; set; }
        public int QuadraKills { get; set; }
        public int PentaKills { get; set; }
        public int GoldEarned { get; set; }
        public int CreepScore { get; set; }
        public int TimeBeingDeadSeconds { get; set; }
    }
}