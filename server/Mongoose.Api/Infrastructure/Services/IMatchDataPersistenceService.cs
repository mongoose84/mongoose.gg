using System.Text.Json;

namespace Mongoose.Api.Infrastructure.Services;

public interface IMatchDataPersistenceService
{
    Task PersistMatchDataAsync(JsonElement matchRoot, JsonElement? timelineRoot);
}