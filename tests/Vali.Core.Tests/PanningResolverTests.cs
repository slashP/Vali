using Shouldly;
using Vali.Core.Google;
using Xunit;

namespace Vali.Core.Tests;

public class PanningResolverTests
{
    private const string In = PanningResolver.InDrivingDirection;
    private const string Away = PanningResolver.AwayFromDrivingDirection;

    [Theory]
    [InlineData(10, 350, 20)]
    [InlineData(350, 10, 20)]
    [InlineData(0, 180, 180)]
    [InlineData(90, 90, 0)]
    [InlineData(359, 1, 2)]
    public void AngleDiff_is_the_short_way_round(int a, int b, int expected) =>
        PanningResolver.AngleDiff(a, b).ShouldBe((decimal)expected);

    [Fact]
    public void Opposite_swaps_the_two_directions()
    {
        PanningResolver.Opposite(In).ShouldBe(Away);
        PanningResolver.Opposite(Away).ShouldBe(In);
    }

    // The old picker compared |arrow - drivingDirection| without wrap-around, so a car heading
    // just west of north got its forward and backward arrows swapped.
    [Theory]
    [InlineData(358, 2, 182)]
    [InlineData(5, 350, 170)]
    [InlineData(10, 355, 175)]
    [InlineData(180, 178, 358)]
    public void Two_arrows_pick_forward_and_backward_across_north(int drivingDirection, int forward, int backward)
    {
        decimal[] arrows = [forward, backward];
        PanningResolver.PickHeading(arrows, drivingDirection, In, new Random(1)).ShouldBe((decimal)forward);
        PanningResolver.PickHeading(arrows, drivingDirection, Away, new Random(1)).ShouldBe((decimal)backward);
    }

    [Fact]
    public void More_than_two_arrows_use_the_most_opposite_pair_across_north()
    {
        decimal[] arrows = [1, 90, 181];
        PanningResolver.PickHeading(arrows, 359, In, new Random(1)).ShouldBe(1m);
        PanningResolver.PickHeading(arrows, 359, Away, new Random(1)).ShouldBe(181m);
    }

    [Fact]
    public void No_arrows_returns_the_driving_direction() =>
        PanningResolver.PickHeading([], 123, In, new Random(1)).ShouldBe(123m);

    [Fact]
    public void One_arrow_is_used_whatever_the_panning() =>
        PanningResolver.PickHeading([200], 10, In, new Random(1)).ShouldBe(200m);

    [Fact]
    public void No_panning_picks_one_of_the_arrows()
    {
        decimal[] arrows = [20, 200];
        PanningResolver.PickHeading(arrows, 20, null, new Random(1)).ShouldBeOneOf(20m, 200m);
    }

    private static PanningContext Context(int resolutionHeight = 8192, string country = "US") =>
        new("pano", country, null, 40, -100, 2025, 5, resolutionHeight);

    private static Dictionary<string, CountryPanning?> Rules(CountryPanning panning) => new() { ["US"] = panning };

    private static string? Resolve(Dictionary<string, CountryPanning?> rules, PanningContext context, int seed = 1,
        Func<PanningContext, PanningDecision?>? hook = null) =>
        PanningResolver.ResolvePanning(rules, hook, context, new Random(seed));

    [Fact]
    public void A_country_without_a_rule_has_no_panning() =>
        Resolve(Rules(new CountryPanning { DefaultPanning = In }), Context(country: "CA")).ShouldBeNull();

    [Fact]
    public void Percentage_100_always_uses_the_rule()
    {
        var rules = Rules(new CountryPanning { DefaultPanning = Away, Percentage = 100 });
        Enumerable.Range(0, 50).Select(seed => Resolve(rules, Context(), seed)).ShouldAllBe(p => p == Away);
    }

    [Fact]
    public void Missing_percentage_means_100()
    {
        var rules = Rules(new CountryPanning { DefaultPanning = Away });
        Enumerable.Range(0, 50).Select(seed => Resolve(rules, Context(), seed)).ShouldAllBe(p => p == Away);
    }

    [Fact]
    public void Percentage_0_always_uses_the_opposite()
    {
        var rules = Rules(new CountryPanning { DefaultPanning = Away, Percentage = 0 });
        Enumerable.Range(0, 50).Select(seed => Resolve(rules, Context(), seed)).ShouldAllBe(p => p == In);
    }

    [Fact]
    public void Percentage_75_keeps_the_rule_about_three_times_in_four()
    {
        var rules = Rules(new CountryPanning { DefaultPanning = Away, Percentage = 75 });
        var rng = new Random(42);
        var kept = Enumerable.Range(0, 2000).Count(_ => PanningResolver.ResolvePanning(rules, null, Context(), rng) == Away);
        kept.ShouldBeInRange(1400, 1600);
    }

    [Fact]
    public void A_matching_expression_wins_and_uses_its_own_percentage()
    {
        var rules = Rules(new CountryPanning
        {
            DefaultPanning = Away,
            Percentage = 100,
            PanningExpressions = [new PanningExpression { Expression = "resolutionHeight lt 8192", Panning = In, Percentage = 0 }]
        });
        Resolve(rules, Context(resolutionHeight: 6656)).ShouldBe(Away); // expression "in" at 0% → opposite
        Resolve(rules, Context(resolutionHeight: 8192)).ShouldBe(Away); // no match → default
    }

    [Fact]
    public void A_non_null_hook_beats_the_rules()
    {
        var rules = Rules(new CountryPanning { DefaultPanning = Away });
        Resolve(rules, Context(), hook: _ => new PanningDecision(In, 100)).ShouldBe(In);
    }

    [Fact]
    public void A_null_hook_falls_through_to_the_rules()
    {
        var rules = Rules(new CountryPanning { DefaultPanning = Away });
        Resolve(rules, Context(), hook: _ => null).ShouldBe(Away);
    }

    [Fact]
    public void The_hook_sees_the_context()
    {
        PanningContext? seen = null;
        var context = new PanningContext("abc", "US", "US-AK", 61, -150, 2025, 8, 8192);
        PanningResolver.ResolvePanning(null, c => { seen = c; return null; }, context, new Random(1));
        seen.ShouldBe(context);
    }

    [Fact]
    public void Seeded_random_is_stable_for_a_pano()
    {
        PanningResolver.SeededRandom("AJp1Q1YYFTQ46vZ3V1QqSw").Next()
            .ShouldBe(PanningResolver.SeededRandom("AJp1Q1YYFTQ46vZ3V1QqSw").Next());
        var rules = Rules(new CountryPanning { DefaultPanning = Away, Percentage = 50 });
        var first = PanningResolver.ResolvePanning(rules, null, Context(), PanningResolver.SeededRandom("pano-1"));
        var second = PanningResolver.ResolvePanning(rules, null, Context(), PanningResolver.SeededRandom("pano-1"));
        second.ShouldBe(first);
    }
}
