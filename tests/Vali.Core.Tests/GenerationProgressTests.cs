using Shouldly;
using Vali.Core.Generation;
using Xunit;

namespace Vali.Core.Tests;

public class GenerationProgressTests
{
    private static (IList<Location> locations, int regionGoalCount, int minDistance)[] Result(
        string subdivision, int produced, int goal) =>
    [
        (Enumerable.Range(0, produced).Select(_ => new Location
        {
            Google = new GoogleData(),
            Osm = new OsmData(),
            Nominatim = new NominatimData { SubdivisionCode = subdivision }
        }).ToList<Location>(), goal, 0)
    ];

    [Fact]
    public void Registers_country_as_queued_with_zero_completed()
    {
        var p = new GenerationProgress();
        p.RegisterCountry("DE", 3);
        var c = p.Snapshot().Countries.Single();
        c.Code.ShouldBe("DE");
        c.Completed.ShouldBe(0);
        c.Total.ShouldBe(3);
        c.Status.ShouldBe(CountryStatus.Queued);
    }

    [Fact]
    public void Partial_completion_is_working_full_completion_is_done()
    {
        var p = new GenerationProgress();
        p.RegisterCountry("DE", 2);
        p.ReportCompleted("DE", "DE-1", Result("DE-1", 10, 10));
        p.Snapshot().Countries.Single().Status.ShouldBe(CountryStatus.Working);
        p.ReportCompleted("DE", "DE-2", Result("DE-2", 10, 10));
        p.Snapshot().Countries.Single().Status.ShouldBe(CountryStatus.Done);
    }

    [Fact]
    public void Done_country_with_a_qualifying_short_region_is_marked_short()
    {
        var p = new GenerationProgress();
        p.RegisterCountry("CO", 1);
        p.ReportCompleted("CO", "CO-GUV", Result("CO-GUV", 4, 11)); // short 7, qualifies
        p.Snapshot().Countries.Single().Status.ShouldBe(CountryStatus.Short);
    }

    [Fact]
    public void Country_with_zero_work_items_is_immediately_done()
    {
        var p = new GenerationProgress();
        p.RegisterCountry("XX", 0);
        var c = p.Snapshot().Countries.Single();
        c.Status.ShouldBe(CountryStatus.Done);
    }

    [Fact]
    public void Snapshot_aggregates_totals()
    {
        var p = new GenerationProgress();
        p.RegisterCountry("DE", 1);
        p.RegisterCountry("FR", 1);
        p.ReportCompleted("DE", "DE-1", Result("DE-1", 16, 16));
        var snap = p.Snapshot();
        snap.CountriesTotal.ShouldBe(2);
        snap.CountriesComplete.ShouldBe(1);
        snap.TotalLocations.ShouldBe(16);
    }

    [Fact]
    public void Snapshot_sums_the_registered_goal()
    {
        var p = new GenerationProgress();
        p.RegisterCountry("DE", 1, goal: 1000);
        p.RegisterCountry("FR", 1, goal: 1500);
        p.Snapshot().TotalGoal.ShouldBe(2500);
    }

    [Fact]
    public void Snapshot_is_immutable_against_later_reports()
    {
        var p = new GenerationProgress();
        p.RegisterCountry("DE", 2);
        p.ReportCompleted("DE", "DE-1", Result("DE-1", 5, 10));
        var snap = p.Snapshot();
        p.ReportCompleted("DE", "DE-2", Result("DE-2", 5, 10));
        snap.TotalLocations.ShouldBe(5); // unchanged by the later report
        snap.Countries.Single().Completed.ShouldBe(1);
    }

    [Fact]
    public async Task Concurrent_reports_produce_correct_totals()
    {
        var p = new GenerationProgress();
        const int n = 200;
        p.RegisterCountry("DE", n);
        await Task.WhenAll(Enumerable.Range(0, n).Select(_ =>
            Task.Run(() => p.ReportCompleted("DE", "DE-X", Result("DE-X", 1, 1)))));
        var snap = p.Snapshot();
        snap.Countries.Single().Completed.ShouldBe(n);
        snap.Countries.Single().Status.ShouldBe(CountryStatus.Done);
        snap.TotalLocations.ShouldBe(n);
        p.Outcomes().Count.ShouldBe(n);
    }

    [Fact]
    public void Zero_yield_result_uses_the_work_item_subdivision_code()
    {
        var p = new GenerationProgress();
        p.RegisterCountry("NG", 1);
        // 0 produced -> no location to read the code from; the work item supplies it.
        p.ReportCompleted("NG", "NG-LA", Result("", 0, 51));
        var outcome = p.Outcomes().Single();
        outcome.SubdivisionCode.ShouldBe("NG-LA");
        outcome.Produced.ShouldBe(0);
        outcome.Goal.ShouldBe(51);
    }

    [Fact]
    public void Outcome_falls_back_to_location_code_when_no_work_item_code_is_given()
    {
        var p = new GenerationProgress();
        p.RegisterCountry("DE", 1);
        // Multi-subdivision work items pass null and rely on the produced locations.
        p.ReportCompleted("DE", null, Result("DE-1", 3, 10));
        p.Outcomes().Single().SubdivisionCode.ShouldBe("DE-1");
    }

    [Theory]
    [InlineData(18, 20, false)] // short 2 -> fails >=3
    [InlineData(17, 20, true)]  // short 3 -> boundary, 3*20>=20
    [InlineData(16, 20, true)]  // short 4
    [InlineData(20, 40, true)]  // short 20, -50%
    [InlineData(195, 200, false)] // short 5 -> 5*20=100 < 200, below 5%
    [InlineData(1000, -1, false)] // no goal -> never short
    public void QualifiesAsShort_applies_the_margin(int produced, int goal, bool expected) =>
        GenerationProgress.QualifiesAsShort(produced, goal).ShouldBe(expected);
}
