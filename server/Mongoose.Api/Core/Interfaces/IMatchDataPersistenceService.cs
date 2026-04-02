using System.Text.Json;

namespace Mongoose.Api.Core.Interfaces;

public interface IMatchDataPersistenceService
{
    Task PersistMatchDataAsync(JsonElement matchRoot, JsonElement? timelineRoot);
}
