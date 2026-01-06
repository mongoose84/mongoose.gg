namespace RiotProxy.Application.DTOs
{
    public static class DuoMatchDurationDto
    {
        public record DuoMatchDurationResponse(
            IList<DurationBucket> Buckets
        );

        public record DurationBucket(
            string DurationRange,
            int MinMinutes,
            int MaxMinutes,
            double Winrate,
            int GamesPlayed
        );
    }
}

