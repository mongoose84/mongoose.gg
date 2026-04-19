namespace Mongoose.Api.Core;

public static class DeathThresholds
{
    public static (int Low, int High) ForRank(string? rankTier) => rankTier?.ToUpperInvariant() switch
    {
        "IRON"        => (6, 9),
        "BRONZE"      => (5, 8),
        "SILVER"      => (5, 7),
        "GOLD"        => (4, 6),
        "PLATINUM"    => (4, 6),
        "EMERALD"     => (3, 5),
        "DIAMOND"     => (3, 5),
        "MASTER"      => (3, 5),
        "GRANDMASTER" => (3, 5),
        "CHALLENGER"  => (3, 5),
        _             => (4, 7)  // Unranked / unknown
    };
}
