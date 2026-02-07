using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Application.Services;

/// <summary>
/// Implementation of LP calculation service for League of Legends rank comparisons.
/// Provides methods for converting ranks to numeric values for comparison and delta calculations.
/// </summary>
public sealed class LpCalculationService : ILpCalculationService
{
    /// <inheritdoc />
    public int CalculateAbsoluteLp(string? tier, string? division, int lp)
    {
        var tierValue = GetTierValue(tier);
        var divisionValue = GetDivisionValue(division);

        // For Master+ (no divisions), just add LP directly to tier base
        // For other tiers, add division offset + LP within division
        return tierValue + divisionValue + lp;
    }

    /// <inheritdoc />
    public int GetTierValue(string? tier)
    {
        return tier?.ToUpperInvariant() switch
        {
            "IRON" => 0,
            "BRONZE" => 400,
            "SILVER" => 800,
            "GOLD" => 1200,
            "PLATINUM" => 1600,
            "EMERALD" => 2000,
            "DIAMOND" => 2400,
            "MASTER" => 2800,
            "GRANDMASTER" => 2800, // Same base as Master, differentiated by LP
            "CHALLENGER" => 2800,  // Same base as Master, differentiated by LP
            _ => 0
        };
    }

    /// <inheritdoc />
    public int GetDivisionValue(string? division)
    {
        return division?.ToUpperInvariant() switch
        {
            "IV" => 0,
            "III" => 100,
            "II" => 200,
            "I" => 300,
            _ => 0 // Master+ don't have divisions
        };
    }

    /// <inheritdoc />
    public int GetTierLevel(string? tier)
    {
        return tier?.ToUpperInvariant() switch
        {
            "IRON" => 1,
            "BRONZE" => 2,
            "SILVER" => 3,
            "GOLD" => 4,
            "PLATINUM" => 5,
            "EMERALD" => 6,
            "DIAMOND" => 7,
            "MASTER" => 8,
            "GRANDMASTER" => 9,
            "CHALLENGER" => 10,
            _ => 0
        };
    }

    /// <inheritdoc />
    public int GetDivisionLevel(string? division)
    {
        return division?.ToUpperInvariant() switch
        {
            "IV" => 1,
            "III" => 2,
            "II" => 3,
            "I" => 4,
            _ => 0
        };
    }

    /// <inheritdoc />
    public bool IsPromotion(string? previousTier, string? previousDivision, string currentTier, string currentDivision)
    {
        if (string.IsNullOrEmpty(previousTier)) return false;

        var prevTierLevel = GetTierLevel(previousTier);
        var currTierLevel = GetTierLevel(currentTier);

        if (currTierLevel > prevTierLevel) return true;

        if (currTierLevel == prevTierLevel)
        {
            var prevDivision = GetDivisionLevel(previousDivision);
            var currDivision = GetDivisionLevel(currentDivision);
            return currDivision > prevDivision;
        }

        return false;
    }

    /// <inheritdoc />
    public bool IsDemotion(string? previousTier, string? previousDivision, string currentTier, string currentDivision)
    {
        if (string.IsNullOrEmpty(previousTier)) return false;

        var prevTierLevel = GetTierLevel(previousTier);
        var currTierLevel = GetTierLevel(currentTier);

        if (currTierLevel < prevTierLevel) return true;

        if (currTierLevel == prevTierLevel)
        {
            var prevDivision = GetDivisionLevel(previousDivision);
            var currDivision = GetDivisionLevel(currentDivision);
            return currDivision < prevDivision;
        }

        return false;
    }

    /// <inheritdoc />
    public string FormatRank(string tier, string? division)
    {
        var formattedTier = tier.Length > 0
            ? char.ToUpper(tier[0]) + tier.Substring(1).ToLower()
            : tier;
        return string.IsNullOrEmpty(division) ? formattedTier : $"{formattedTier} {division}";
    }
}

