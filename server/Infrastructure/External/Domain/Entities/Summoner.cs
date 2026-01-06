namespace RiotProxy.External.Domain.Entities
{
    public class Summoner : EntityBase
    {
        public string Puuid { get; set; } = string.Empty;
        public long RevisionDate { get; set; }
        public int ProfileIconId { get; set; }
        public long SummonerLevel { get; set; }  
    }
}