namespace RiotProxy.External.Domain.Entities
{
    public class LolMatch : EntityBase
    {
        public string MatchId { get; set; } = string.Empty;
        
        // Not persisted: used during fetch to keep the owner PUUID context
        public string Puuid { get; set; } = string.Empty;
        public bool InfoFetched { get; set; } = false;
        public string GameMode { get; set; } = string.Empty;
        public long DurationSeconds { get; set; }
        public DateTime GameEndTimestamp { get; set; }
    }
}