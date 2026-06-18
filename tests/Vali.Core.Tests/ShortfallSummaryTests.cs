using Shouldly;
using Vali.Core.Generation;
using Xunit;

namespace Vali.Core.Tests;

public class ShortfallSummaryTests
{
    private static RegionOutcome Out(string country, string sub, int produced, int goal) =>
        new(country, sub, produced, goal, 0);

    [Fact]
    public void Empty_outcomes_means_no_shortfall()
    {
        ShortfallSummary.Select([]).AnyShortfall.ShouldBeFalse();
    }

    [Fact]
    public void Nothing_qualifying_means_no_shortfall()
    {
        var report = ShortfallSummary.Select([Out("DE", "DE-1", 100, 100), Out("DE", "DE-2", 99, 100)]);
        report.AnyShortfall.ShouldBeFalse();
    }

    [Fact]
    public void Filters_subdivisions_by_margin_and_orders_worst_first()
    {
        var report = ShortfallSummary.Select(
        [
            Out("AA", "AA-1", 18, 20),   // short 2  -> excluded
            Out("AA", "AA-2", 17, 20),   // short 3  -> included (boundary)
            Out("AA", "AA-3", 16, 20),   // short 4  -> included
            Out("AA", "AA-4", 20, 40),   // short 20 -> included (-50%)
            Out("AA", "AA-5", 195, 200), // short 5 but < 5% -> excluded
        ]);

        var aa = report.Countries.Single();
        aa.Code.ShouldBe("AA");
        aa.Subdivisions.Select(s => s.Code).ShouldBe(["AA-4", "AA-3", "AA-2"]); // worst-first
        // Country aggregate is over ALL subdivisions.
        aa.Produced.ShouldBe(266);
        aa.Goal.ShouldBe(300);
        aa.Shortfall.ShouldBe(34);
        aa.PctBelow.ShouldBe(11);
    }

    [Fact]
    public void Countries_are_ordered_by_total_qualifying_shortfall()
    {
        var report = ShortfallSummary.Select(
        [
            Out("AA", "AA-1", 16, 20), // qualifying shortfall 4
            Out("AA", "AA-2", 20, 40), // qualifying shortfall 20  -> AA total 24
            Out("BB", "BB-1", 1, 6),   // qualifying shortfall 5   -> BB total 5
        ]);

        report.Countries.Select(c => c.Code).ShouldBe(["AA", "BB"]);
    }

    [Fact]
    public void Zero_yield_region_with_a_real_goal_is_included()
    {
        var report = ShortfallSummary.Select([Out("CC", "", 0, 11)]);
        var cc = report.Countries.Single();
        cc.Subdivisions.Single().Produced.ShouldBe(0);
        cc.Subdivisions.Single().Goal.ShouldBe(11);
    }

    [Fact]
    public void Subdivision_carries_its_human_readable_name()
    {
        var expected = SubdivisionWeights.SubdivisionName("AD", "AD-02");
        expected.ShouldNotBeNullOrEmpty(); // sanity: AD-02 is a real subdivision code

        var report = ShortfallSummary.Select([Out("AD", "AD-02", 0, 50)]);

        report.Countries.Single().Subdivisions.Single().Name.ShouldBe(expected);
    }

    [Fact]
    public void Missing_subdivision_code_has_no_name()
    {
        var report = ShortfallSummary.Select([Out("AD", "", 0, 50)]);
        report.Countries.Single().Subdivisions.Single().Name.ShouldBeNull();
    }

    [Fact]
    public void Unknown_country_does_not_throw_and_has_no_name()
    {
        var report = ShortfallSummary.Select([Out("ZZ", "ZZ-1", 0, 50)]);
        report.Countries.Single().Subdivisions.Single().Name.ShouldBeNull();
    }
}
