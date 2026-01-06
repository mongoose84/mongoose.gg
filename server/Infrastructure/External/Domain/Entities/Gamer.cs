using RiotProxy.Infrastructure.External.Database.Records;

namespace RiotProxy.External.Domain.Entities
{
    public class Gamer : EntityBase
    {
        public string Puuid { get; set; } = string.Empty;
        public string GamerName { get; set; } = string.Empty;
        public string Tagline { get; set; } = string.Empty;
        public int IconId { get; set; }
        public string IconUrl { get; set; } = string.Empty;
        public long Level { get; set; }
        public DateTime LastChecked { get; set; }
        /// <summary>
        /// Details about the most recent game played by this gamer.
        /// Populated at runtime from match data, not stored in the database.
        /// </summary>
        public LatestGameRecord? LatestGame { get; set; }
        public GamerStats Stats { get; set; } = new GamerStats();
    }
}