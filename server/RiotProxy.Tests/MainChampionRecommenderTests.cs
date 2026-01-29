using System.Linq;
using FluentAssertions;
using RiotProxy.Application.Services;
using Xunit;

namespace RiotProxy.Tests;

public class MainChampionRecommenderTests
{
    // Helper to create ChampionRoleStats with default early-game values
    // Early-game stats are nullable - null means no data available
    private static MainChampionRecommender.ChampionRoleStats CreateStats(
        string role, int championId, string championName,
        int gamesPlayed, int wins,
        double avgGoldPerMin = 350.0, double avgCs = 150.0,
        double avgKills = 5.0, double avgDeaths = 3.0, double avgAssists = 4.0,
        double? avgGoldDiff15 = 0.0, double? avgDeathsPre10 = 1.0, double? avgVisionPerMin = 0.8)
    {
        return new MainChampionRecommender.ChampionRoleStats(
            role, championId, championName, gamesPlayed, wins,
            avgGoldPerMin, avgCs, avgKills, avgDeaths, avgAssists,
            avgGoldDiff15, avgDeathsPre10, avgVisionPerMin);
    }

    [Fact]
    public void Includes_champions_with_single_game()
    {
        // No minimum games filter - even 1 game champions should be included
        var stats = new[]
        {
            CreateStats("TOP", 1, "Garen", gamesPlayed: 1, wins: 1),
            CreateStats("TOP", 2, "Darius", gamesPlayed: 3, wins: 2)
        };

        var result = MainChampionRecommender.BuildMainChampionsByRole(stats);

        result.Should().HaveCount(1);
        var topRole = result.Single();
        topRole.Role.Should().Be("TOP");
        topRole.Champions.Should().HaveCount(2); // Both champions included
    }

    [Fact]
    public void Orders_champions_by_recommended_score()
    {
        var stats = new[]
        {
            // Strong performer: high win rate, decent sample, good stats, good laning
            CreateStats("JUNGLE", 1, "CarryJg", gamesPlayed: 20, wins: 16,
                avgKills: 8.0, avgDeaths: 2.0, avgAssists: 10.0,
                avgGoldDiff15: 500, avgDeathsPre10: 0.5, avgVisionPerMin: 1.0),
            // Weaker performer: mediocre win rate, smaller sample, worse stats
            CreateStats("JUNGLE", 2, "OtherJg", gamesPlayed: 10, wins: 5,
                avgKills: 4.0, avgDeaths: 5.0, avgAssists: 6.0,
                avgGoldDiff15: -200, avgDeathsPre10: 2.0, avgVisionPerMin: 0.5)
        };

        var result = MainChampionRecommender.BuildMainChampionsByRole(stats);

        var jungle = result.Single();
        jungle.Champions.Should().HaveCount(2);
        jungle.Champions[0].ChampionId.Should().Be(1); // CarryJg should be first
    }

    [Fact]
    public void High_games_mediocre_winrate_beats_single_game_perfect_winrate()
    {
        // This is the key test: confidence-based scoring should prevent
        // a single lucky win from outranking an experienced champion
        var stats = new[]
        {
            // 1 game, 100% win rate - should NOT be ranked first
            CreateStats("BOTTOM", 1, "MissFortune", gamesPlayed: 1, wins: 1,
                avgKills: 10.0, avgDeaths: 1.0, avgAssists: 5.0,
                avgGoldDiff15: 1000, avgDeathsPre10: 0, avgVisionPerMin: 0.8),
            // 66 games, 47% win rate - should be ranked first due to confidence
            CreateStats("BOTTOM", 2, "Tristana", gamesPlayed: 66, wins: 31,
                avgKills: 6.0, avgDeaths: 4.0, avgAssists: 7.0,
                avgGoldDiff15: 100, avgDeathsPre10: 1.0, avgVisionPerMin: 0.7)
        };

        var result = MainChampionRecommender.BuildMainChampionsByRole(stats);

        var bottom = result.Single();
        bottom.Champions.Should().HaveCount(2);
        // Tristana (66 games) should rank higher than MissFortune (1 game)
        bottom.Champions[0].ChampionId.Should().Be(2, "66 games at 47% WR should beat 1 game at 100% WR");
        bottom.Champions[0].ChampionName.Should().Be("Tristana");
    }

    [Fact]
    public void BuildMainChampionsByRole_IgnoresUnknownRole()
    {
        var stats = new[]
        {
            CreateStats("UNKNOWN", 1, "SomeChamp", gamesPlayed: 20, wins: 10),
            CreateStats("TOP", 2, "Garen", gamesPlayed: 15, wins: 12)
        };

        var result = MainChampionRecommender.BuildMainChampionsByRole(stats);

        result.Should().HaveCount(1);
        result[0].Role.Should().Be("TOP");
    }

    [Fact]
    public void Support_role_weights_vision_more_heavily()
    {
        // For supports, vision should be weighted more heavily than gold diff
        var stats = new[]
        {
            // High vision, low gold diff
            CreateStats("UTILITY", 1, "Thresh", gamesPlayed: 20, wins: 12,
                avgGoldDiff15: -500, avgDeathsPre10: 1.0, avgVisionPerMin: 1.5),
            // Low vision, high gold diff
            CreateStats("UTILITY", 2, "Brand", gamesPlayed: 20, wins: 12,
                avgGoldDiff15: 500, avgDeathsPre10: 1.0, avgVisionPerMin: 0.3)
        };

        var result = MainChampionRecommender.BuildMainChampionsByRole(stats);

        var support = result.Single();
        support.Champions.Should().HaveCount(2);
        // Thresh (high vision) should rank higher for support role
        support.Champions[0].ChampionId.Should().Be(1, "Support should value vision over gold diff");
    }

    [Fact]
    public void Missing_laning_data_uses_neutral_score_not_best_score()
    {
        // This tests that null early-game data (no metrics populated) doesn't
        // artificially inflate scores like defaulting to 0 deaths would
        var stats = new[]
        {
            // Champion with laning data: genuinely 0 deaths pre-10 (excellent)
            CreateStats("MID", 1, "Ahri", gamesPlayed: 20, wins: 12,
                avgGoldDiff15: 200, avgDeathsPre10: 0, avgVisionPerMin: 0.8),
            // Champion without laning data: null values (should use neutral scores)
            CreateStats("MID", 2, "Syndra", gamesPlayed: 20, wins: 12,
                avgGoldDiff15: null, avgDeathsPre10: null, avgVisionPerMin: null)
        };

        var result = MainChampionRecommender.BuildMainChampionsByRole(stats);

        var mid = result.Single();
        mid.Champions.Should().HaveCount(2);
        // Ahri (with genuine 0 deaths = excellent laning) should rank higher
        // than Syndra (missing data = neutral score)
        mid.Champions[0].ChampionId.Should().Be(1, "Champion with genuine 0 deaths should beat champion with missing data");
    }
}

