namespace RiotProxy.Application.DTOs
{
    public static class DuoImprovementSummaryDto
    {
        public record DuoImprovementSummaryResponse(
            IList<Insight> Insights
        );

        public record Insight(
            string Type,
            string Text
        );
    }
}

