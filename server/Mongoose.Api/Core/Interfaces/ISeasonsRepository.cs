using Mongoose.Api.Core.Entities;

namespace Mongoose.Api.Core.Interfaces;

public interface ISeasonsRepository
{
    Task UpsertAsync(Season season);
    Task<Season?> GetByCodeAsync(string seasonCode);
}

