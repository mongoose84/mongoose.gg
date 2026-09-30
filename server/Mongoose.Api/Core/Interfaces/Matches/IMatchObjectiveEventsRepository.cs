using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

public interface IMatchObjectiveEventsRepository
{
    /// <summary>Replaces the match's objective events with <paramref name="events"/>, in one transaction.</summary>
    Task ReplaceForMatchAsync(string matchId, IReadOnlyList<MatchObjectiveEvent> events);
}
