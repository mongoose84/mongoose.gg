using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

public interface IMatchGoldFramesRepository
{
    /// <summary>Replaces the match's gold frames with <paramref name="frames"/>, in one transaction.</summary>
    Task ReplaceForMatchAsync(string matchId, IReadOnlyList<MatchGoldFrame> frames);
}
