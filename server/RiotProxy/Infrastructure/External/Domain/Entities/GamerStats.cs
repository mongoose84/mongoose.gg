namespace RiotProxy.External.Domain.Entities
{
    public class GamerStats : EntityBase
    {
        public int TotalMatches { get; set; }
        public int Wins { get; set; }
        public int TotalKills { get; set; }
        public int TotalDeaths { get; set; }
        public int TotalAssists { get; set; }
        public int TotalCreepScore { get; set; }
        public int TotalGoldEarned { get; set; }
        public long TotalDurationPlayedSeconds { get; set; }
    }
}