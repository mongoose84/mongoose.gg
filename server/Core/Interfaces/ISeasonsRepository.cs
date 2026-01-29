using RiotProxy.Core.Entities;

namespace RiotProxy.Core.Interfaces;

public interface ISeasonsRepository
{
    Task UpsertAsync(Season season);
    Task<Season?> GetByCodeAsync(string seasonCode);
}

