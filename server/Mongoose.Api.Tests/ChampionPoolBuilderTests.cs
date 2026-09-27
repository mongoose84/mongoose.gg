using System.Linq;
using FluentAssertions;
using Mongoose.Api.Core.QueryModels;
using Mongoose.Api.Core.Services;
using Xunit;

namespace Mongoose.Api.Tests;

public class ChampionPoolBuilderTests
{
    // Every champion starts at the same "average" line, so no metric leads unless a test says so
    private static ChampionPoolStatsData Champ(
        int id, string name, int games, int wins,
        double avgKills = 5.0, double avgDeaths = 4.0, double avgAssists = 7.0,
        double avgCsPerMin = 7.0, double? avgGoldDiff15 = 0.0,
        double? avgDeathsPre10 = 1.0, double? avgVisionPerMin = 1.0,
        double? avgDamageSharePct = 25.0, double? avgKillParticipationPct = 50.0,
        long lastPlayed = 1_000)
    {
        return new ChampionPoolStatsData(
            id, name, games, wins,
            avgKills, avgDeaths, avgAssists, avgCsPerMin,
            avgGoldDiff15, avgGoldDiff15.HasValue ? games : 0,
            avgDeathsPre10, avgVisionPerMin, avgDamageSharePct, avgKillParticipationPct,
            games, lastPlayed);
    }

    private static ChampionRoleCountData Role(int id, string role, int games, long lastPlayed = 1_000)
        => new(id, role, games, lastPlayed);

    [Fact]
    public void Returns_empty_pool_for_no_champions()
    {
        var pool = ChampionPoolBuilder.Build([], []);

        pool.Champions.Should().BeEmpty();
        pool.AlsoPlayed.Should().BeEmpty();
    }

    [Fact]
    public void Orders_by_m_score_not_by_matches()
    {
        var champions = new[]
        {
            // Many matches, losing
            Champ(1, "Garen", games: 30, wins: 11),
            // Fewer matches, winning
            Champ(2, "Darius", games: 20, wins: 13),
        };

        var pool = ChampionPoolBuilder.Build(champions, []);

        pool.Champions.Select(c => c.ChampionName).Should().Equal("Darius", "Garen");
        pool.Champions[0].MScore.Should().BeGreaterThan(pool.Champions[1].MScore);
    }

    [Fact]
    public void Splits_top_three_cards_and_next_three_also_played()
    {
        var champions = Enumerable.Range(1, 8)
            .Select(i => Champ(i, $"Champ{i}", games: 20, wins: 20 - i))
            .ToArray();

        var pool = ChampionPoolBuilder.Build(champions, []);

        pool.Champions.Select(c => c.ChampionId).Should().Equal(1, 2, 3);
        pool.AlsoPlayed.Select(c => c.ChampionId).Should().Equal(4, 5, 6);
        pool.AlsoPlayed.Should().OnlyContain(c => c.StrengthTag == null);
    }

    [Fact]
    public void Computes_win_rate_and_kda()
    {
        var pool = ChampionPoolBuilder.Build([Champ(1, "Ahri", games: 22, wins: 14, avgKills: 6, avgDeaths: 3, avgAssists: 6.3)], []);

        var ahri = pool.Champions.Single();
        ahri.Matches.Should().Be(22);
        ahri.Wins.Should().Be(14);
        ahri.WinRate.Should().Be(63.6);
        ahri.AvgKda.Should().Be(4.1);
    }

    [Fact]
    public void Uses_the_role_with_most_matches_as_primary_role()
    {
        var roles = new[]
        {
            Role(1, "MIDDLE", 4, lastPlayed: 5_000),
            Role(1, "TOP", 9, lastPlayed: 1_000),
        };

        var pool = ChampionPoolBuilder.Build([Champ(1, "Sylas", games: 13, wins: 7)], roles);

        pool.Champions.Single().Role.Should().Be("TOP");
    }

    [Fact]
    public void Breaks_primary_role_ties_by_most_recent_match()
    {
        var roles = new[]
        {
            Role(1, "TOP", 5, lastPlayed: 1_000),
            Role(1, "MIDDLE", 5, lastPlayed: 5_000),
        };

        var pool = ChampionPoolBuilder.Build([Champ(1, "Sylas", games: 10, wins: 5)], roles);

        pool.Champions.Single().Role.Should().Be("MIDDLE");
    }

    [Fact]
    public void Gives_each_card_its_strongest_metric_as_tag()
    {
        var champions = new[]
        {
            Champ(1, "Ahri", games: 20, wins: 12, avgGoldDiff15: 600),
            Champ(2, "Syndra", games: 20, wins: 11, avgDamageSharePct: 35),
            Champ(3, "Viktor", games: 20, wins: 10, avgCsPerMin: 9.5),
        };

        var pool = ChampionPoolBuilder.Build(champions, []);

        pool.Champions.Single(c => c.ChampionName == "Ahri").StrengthTag.Should().Be("Best laning");
        pool.Champions.Single(c => c.ChampionName == "Syndra").StrengthTag.Should().Be("Most damage");
        pool.Champions.Single(c => c.ChampionName == "Viktor").StrengthTag.Should().Be("Best farming");
    }

    [Fact]
    public void Never_gives_two_cards_the_same_tag()
    {
        var champions = new[]
        {
            // Both lead on damage (baseline 35%): Syndra +29%, Brand +14%. Brand's involvement lead (+12.5%)
            // is smaller than his damage lead, so he only gets it because Syndra already holds "Most damage".
            Champ(1, "Syndra", games: 20, wins: 12, avgDamageSharePct: 45),
            Champ(2, "Brand", games: 20, wins: 11, avgDamageSharePct: 40, avgKillParticipationPct: 60),
            Champ(3, "Lux", games: 20, wins: 10, avgDamageSharePct: 20),
        };

        var pool = ChampionPoolBuilder.Build(champions, []);

        pool.Champions.Single(c => c.ChampionName == "Syndra").StrengthTag.Should().Be("Most damage");
        pool.Champions.Single(c => c.ChampionName == "Brand").StrengthTag.Should().Be("Most involved");
        pool.Champions.Select(c => c.StrengthTag).Where(t => t != null).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Gives_no_tag_below_five_matches()
    {
        var champions = new[]
        {
            Champ(1, "Garen", games: 30, wins: 16),
            Champ(2, "Zed", games: 4, wins: 4, avgDamageSharePct: 45),
        };

        var pool = ChampionPoolBuilder.Build(champions, []);

        pool.Champions.Single(c => c.ChampionName == "Zed").StrengthTag.Should().BeNull();
    }

    [Fact]
    public void Gives_no_tag_when_nothing_is_ten_percent_above_average()
    {
        var champions = new[]
        {
            Champ(1, "Garen", games: 20, wins: 11, avgCsPerMin: 7.3),
            Champ(2, "Darius", games: 20, wins: 10, avgCsPerMin: 6.9),
        };

        var pool = ChampionPoolBuilder.Build(champions, []);

        pool.Champions.Should().OnlyContain(c => c.StrengthTag == null);
    }

    [Fact]
    public void Measures_laning_lead_in_gold_not_relative_to_a_zero_baseline()
    {
        var champions = new[]
        {
            // Baseline gold diff is about -100; +150 gold is a 250-gold lead (0.25), above the 100-gold floor
            Champ(1, "Ahri", games: 10, wins: 6, avgGoldDiff15: 150),
            Champ(2, "Orianna", games: 10, wins: 5, avgGoldDiff15: -350),
        };

        var pool = ChampionPoolBuilder.Build(champions, []);

        pool.Champions.Single(c => c.ChampionName == "Ahri").StrengthTag.Should().Be("Best laning");
    }

    [Fact]
    public void Skips_metrics_without_data()
    {
        var champions = new[]
        {
            Champ(1, "Ahri", games: 20, wins: 12, avgGoldDiff15: null, avgDamageSharePct: null,
                avgVisionPerMin: null, avgKillParticipationPct: null),
        };

        var act = () => ChampionPoolBuilder.Build(champions, []);

        act.Should().NotThrow();
        act().Champions.Single().StrengthTag.Should().BeNull();
    }
}
